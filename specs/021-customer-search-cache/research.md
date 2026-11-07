# Research: Busca de clientes em memória

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-11-07

## Situação atual (levantada no código)

- `GET /customers?name=` → `CustomerQueryHandler.SearchByName` → `CustomerQueryRepository.GetCustomerByName`
  faz `WHERE Name LIKE '%TERMO%' ORDER BY Id DESC` com `Take(21)`. O `%` à esquerda impede o uso de
  índice, então cada busca percorre a tabela inteira.
- A busca devolve o `CustomerDto` completo. A tela usa o nome e o telefone na lista. Ao clicar,
  `CustomerPickerComponent.openExisting` abre o `CustomerDialogComponent` com o objeto que veio da busca.
- Clientes são gravados **apenas** em `CustomerCommandRepository.CreateAsync`/`UpdateAsync`,
  chamados só por `CustomerCommandHandler.Create`/`Update`. `ServiceOrderCommandRepository` lê o
  cliente, não grava. Não existe exclusão de cliente.
- Não existe `GET /customers/{id}`.
- A API roda em uma instância (`docker-compose.yml`, `mem_limit: 512m`).

## Decisões

### R1. Onde guardar a lista: `IMemoryCache`, numa entrada única com o snapshot inteiro

- **Decision**: uma entrada no `IMemoryCache` (chave `customers:search-index`) contendo um array
  imutável de `CustomerSearchEntry`, já ordenado por `Id` decrescente. A entrada expira em 1 hora
  (`AbsoluteExpirationRelativeToNow`), o que cumpre a recarga periódica do FR-009.
- **Rationale**: foi o que o usuário pediu. O `IMemoryCache` já vem com o ASP.NET Core e com o EF
  Core (`Microsoft.Extensions.Caching.Memory`), então não entra pacote novo. Um array imutável pode
  ser lido por várias requisições ao mesmo tempo sem lock.
- **Alternatives considered**:
  - *Uma entrada por cliente*: não serve para busca por "contém", que precisa percorrer todos.
  - *Índice full-text no SQL Server*: exige migration e não resolve acento nem "contém" no meio
    da palavra tão bem quanto a busca em memória. Seria mais complexo.
  - *Redis / cache distribuído*: desnecessário com uma única instância. Seria nova infraestrutura,
    o que pede justificativa na constituição.

### R2. Invalidação ao criar/editar cliente (pedido explícito do usuário)

- **Decision**: depois de `CreateAsync`/`UpdateAsync` concluírem sem exceção,
  `CustomerCommandHandler` chama `ICustomerSearchCache.Invalidate()`, que **remove a entrada
  inteira** do cache. A busca seguinte remonta o snapshot a partir do banco.
- **Rationale**:
  - É o que o usuário pediu ("invalidar o cache quando criar/editar um cliente").
  - É a forma mais simples de garantir o FR-005: a busca seguinte sempre lê o banco já gravado.
  - O custo é baixo. Cadastros e edições são poucas dezenas por dia, e remontar o snapshot é uma
    consulta só de `Id, Name, Phone` em cerca de 30 mil linhas (estimativa: menos de 300 ms; ver R6).
  - Ficar no **handler** (Application), e não no repositório, permite testar com fakes
    (Princípio IV) e mantém a regra "gravou → invalida" junto do caso de uso.
  - Como a chamada vem depois do `await` do repositório, uma gravação que lança exceção não
    invalida. Isso não afetaria a correção, mas evita uma recarga à toa.
- **Alternatives considered**:
  - *Atualizar o snapshot incrementalmente* (inserir ou trocar só o cliente alterado): evita a
    recarga, mas traz cópia do array, ordenação e concorrência entre duas edições. A spec aceita
    uma busca um pouco mais lenta logo após uma gravação (SC-001 é p95). Fica como otimização
    futura se a métrica mostrar necessidade.
  - *Invalidar dentro do `CustomerCommandRepository`*: funcionaria, mas o repositório não é
    testado por unidade e a regra ficaria escondida na Infrastructure.

### R3. Corrida entre recarga em andamento e invalidação

- **Problema**: a busca A começa a remontar o snapshot e lê o banco. Antes de A terminar, um
  cliente é criado e `Invalidate()` remove a entrada, que ainda nem existe. A termina e grava no
  cache um snapshot **sem** o cliente novo, que fica errado por até 1 hora. Isso viola o FR-005.
- **Decision**: o cache mantém um contador de **geração** (`long`, `Interlocked`). `Invalidate()`
  incrementa a geração e remove a entrada. Quem monta o snapshot guarda a geração antes de ler o
  banco e só grava no cache se a geração não mudou. Se mudou, devolve o resultado para a própria
  requisição, mas não grava. A próxima busca remonta com o dado novo.
- **Rationale**: elimina a janela de dado velho com poucas linhas de código, sem lock global
  durante a leitura.
- **Alternatives considered**: `CancellationChangeToken` do `IMemoryCache`. Ele expira entradas
  já gravadas, mas não cobre a corrida "invalida antes de gravar".

### R4. Montagem única (FR-007) e falha na montagem (FR-008)

- **Decision**: um `SemaphoreSlim(1,1)` serializa a montagem. Depois de entrar, quem montaria
  confere o cache de novo (double-check). Assim, buscas simultâneas após um reinício esperam a
  mesma montagem. Se a consulta ao banco lançar exceção, nada é gravado no cache e a exceção sobe
  para o `ExceptionMiddleware` existente, que responde 500 com mensagem em português. A próxima
  busca tenta de novo.
- **Rationale**: é o padrão conhecido para evitar o "estouro de manada" no cache. `GetOrCreateAsync`
  do `IMemoryCache` não garante execução única.

### R5. Regras de comparação (FR-002, FR-011): normalização feita uma vez, na montagem

- **Decision**: a função `CustomerNameNormalizer.Normalize(string)` fica na **Application**:
  1. `Trim` e troca de qualquer sequência de espaços por um espaço só;
  2. decomposição Unicode (`NormalizationForm.FormD`) e remoção das marcas `NonSpacingMark`
     (tira os acentos; `Ç` vira `C`);
  3. `ToUpperInvariant`.
  
  Cada `CustomerSearchEntry` guarda o `SearchKey` já normalizado. O termo digitado passa pela mesma
  função, e a correspondência é `SearchKey.Contains(termo, StringComparison.Ordinal)`. Como `%` e
  `_` não têm significado especial em `Contains`, o FR-011 é atendido automaticamente.
- **Rationale**: normalizar 30 mil nomes uma vez por montagem é barato. Normalizar a cada busca
  multiplicaria o custo. Com comparação ordinal sobre chaves já normalizadas, percorrer tudo leva
  poucos milissegundos. A regra é pura e testável (Princípio IV).
- **Alternatives considered**: `CompareInfo.IndexOf` com `CompareOptions.IgnoreNonSpace |
  IgnoreCase`. Funciona sem guardar a chave, mas é bem mais lento por comparação e depende de
  cultura (ICU no container Linux).

### R6. Custo de memória e de montagem

- **Estimativa**: cada entrada tem `long` + nome (~30 caracteres) + chave normalizada (~30) +
  telefone (~11) + overhead de objeto. São cerca de 250 bytes. 30 mil entradas ≈ **7,5 MB**, e
  60 mil ≈ 15 MB. Fica dentro do SC-005 (≤ 20 MB) e do `mem_limit: 512m`.
- **Montagem**: `SELECT Id, Name, Phone FROM Customers ORDER BY Id DESC` com `AsNoTracking` e
  projeção, sem materializar a entidade nem os value objects. É uma leitura sequencial de 30 mil
  linhas estreitas, na casa de 100–300 ms. Fica dentro do SC-006 (3 s). O valor real deve ser
  medido no quickstart.
- **Busca**: percorre o array em ordem (já está por `Id` desc) e para quando junta 21 resultados.
  Com termos comuns termina cedo. No pior caso (termo raro) passa pelas 30 mil chaves em
  aproximadamente 1–3 ms.

### R7. Ficha completa ao selecionar (FR-004): novo `GET /customers/{id}`

- **Decision**: criar `GET /customers/{id:long}` → `CustomerQueryHandler.GetById` →
  `ICustomerQueryRepository.GetByIdAsync` (que já existe), respondendo com o `CustomerDto`
  completo, ou 404 com a mensagem existente "O cliente com id:{id} não existe". O frontend chama
  esse endpoint ao clicar no resultado e abre o diálogo com a resposta.
- **Rationale**: a lista em memória só tem ID, nome e telefone. A ficha precisa estar atualizada
  e completa, e vem do banco por chave primária, que é barato.

### R8. Formato da resposta da busca

- **Decision**: `GET /customers?name=` passa a devolver `CustomerSearchItemDto { id, name, phone }`
  em vez do `CustomerDto` completo. A rota, os parâmetros, o limite de 21 itens e a mensagem de
  400 ("Digite pelo menos 3 letras.") continuam iguais. O único consumidor é o
  `CustomerSearchComponent` (verificado por grep em `web/src`).
- **Rationale**: devolver o DTO completo obrigaria guardar todos os campos em memória, o que
  contraria o pedido.

### R9. Onde fica a implementação do cache (Princípios I e II)

- **Decision**:
  - **Application**: `ICustomerSearchCache` (interface com `GetEntriesAsync()` e `Invalidate()`),
    `CustomerSearchEntry`, `CustomerNameNormalizer` e a regra de busca no `CustomerQueryHandler`.
  - **Infrastructure**: `CustomerSearchCache` (singleton) com `IMemoryCache`, geração, semáforo e
    um `IServiceScopeFactory` para abrir um `ApplicationDataContext` com escopo próprio na montagem,
    já que um singleton não pode receber o `DbContext` scoped.
  - **Api**: registro no DI (`AddMemoryCache`, `AddSingleton<ICustomerSearchCache, CustomerSearchCache>`)
    e o novo endpoint.
  - **Domain**: sem mudanças. O método `GetCustomerByName` sai de `ICustomerQueryRepository`
    porque fica sem uso.
- **Rationale**: as dependências apontam para dentro. O Domain não conhece cache. A regra de busca
  fica na Application e é testada com um fake de `ICustomerSearchCache`.

### R10. Observabilidade

- **Decision**: registrar em log `Information` cada montagem do snapshot, com quantidade de
  clientes e duração em ms, e `Debug` a cada invalidação. Assim dá para ver no Loki com que
  frequência o cache é remontado e quanto custa.

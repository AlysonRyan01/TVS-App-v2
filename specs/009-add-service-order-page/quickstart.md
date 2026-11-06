# Quickstart: tela "Adicionar OS"

## 1. Build, testes e migration

```bash
dotnet build TVSApp.sln
dotnet test src/TVSApp.Tests
dotnet ef migrations list --project src/TVSApp.Infrastructure --startup-project src/TVSApp.Api
cd web && npx ng build
```

Conferir que a migration `RenameCustomerCpfToDocument` só tem `RenameColumn` + `AlterColumn`
(nenhum `DropColumn`/`AddColumn`). Aplicar numa cópia do banco:

```bash
dotnet ef database update --project src/TVSApp.Infrastructure --startup-project src/TVSApp.Api
```

Esperado: `Customers.Document` existe, aceita NULL, e os CPFs antigos continuam lá.

## 2. Rotas antigas eliminadas

```bash
grep -rnE "get-customer-by-name|create-customer|update-customer|get-address-by-zip-code|create-service-order\"" src web/src --include=*.cs --include=*.ts
```

Esperado: nenhuma saída.

## 3. Fluxo na tela (API + Angular rodando, usuário logado)

1. Menu → **Adicionar OS** → abre `/adicionar` com duas colunas (cliente | aparelho).
2. Digitar 2 letras → "Digite pelo menos 3 letras."; digitar 3+ letras de um cliente existente →
   lista com nome, telefone e CPF/CNPJ.
3. Escolher o cliente → modal central com os dados → **Confirmar** → cartão do cliente.
4. **Trocar cliente** → volta a busca e os campos do aparelho continuam preenchidos.
5. Escolher de novo, alterar o telefone → **Salvar e usar** → "Cliente atualizado.".
6. Preencher modelo e série → **Criar OS** → o termo de entrada abre com impressão automática;
   "OS {n} criada."; a página volta ao início; a OS aparece em "Orçamentos pendentes" (inclusive em
   outra tela aberta, via SignalR); WhatsApp de boas-vindas no log do worker.
7. Buscar um nome inexistente → "Nenhum cliente encontrado." → **Cadastrar novo cliente** (o nome
   vem preenchido) → CEP preenche rua, bairro, cidade e UF → sem CPF/CNPJ → **Cadastrar e usar**.
8. Repetir com CNPJ alfanumérico (ex.: `12.ABC.345/01DE-35`) → aceito; com `123` → mensagem
   "Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres).".
9. Tela estreita: as colunas se empilham e "Criar OS" continua fixo no rodapé.

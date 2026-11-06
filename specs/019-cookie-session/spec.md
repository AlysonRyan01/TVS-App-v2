# Feature Specification: Login lembrado por cookie seguro

**Feature Branch**: `019-cookie-session`

**Created**: 2026-11-06

**Status**: Draft

**Input**: User description: "trocar sessionStorage por Cookie"


## Contexto

Hoje o sistema guarda o login no armazenamento da aba: ele some ao fechar a aba e cada aba nova pede
login. Além disso, as credenciais ficam legíveis por qualquer script da página. A mudança é guardar a
sessão num **cookie** emitido pelo servidor, que o navegador mantém entre abas e reinícios e que os
scripts da página **não conseguem ler**, reduzindo o risco de roubo de sessão.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuar logado depois de fechar a aba (Priority: P1)

O atendente entra no sistema, fecha a aba, o navegador ou desliga o computador e, ao abrir o sistema de
novo no mesmo navegador, já cai na tela inicial, sem digitar e-mail e senha.

**Why this priority**: é o problema que motivou a mudança; afeta todos os usuários todos os dias.

**Independent Test**: entrar, fechar a aba, reabrir o endereço do sistema → tela inicial sem login.

**Acceptance Scenarios**:

1. **Given** um usuário logado, **When** fecha a aba e reabre o sistema, **Then** vê a tela inicial.
2. **Given** um usuário logado, **When** fecha o navegador, reinicia o computador e abre o sistema
   dentro do prazo da sessão, **Then** continua logado.
3. **Given** um usuário que ficou mais de 1 hora sem usar, **When** volta ao sistema, **Then** o acesso é
   renovado automaticamente, sem digitar a senha.
4. **Given** um usuário logado numa aba, **When** abre o sistema em outra aba, **Then** a nova aba já
   está logada.

---

### User Story 2 - Sessão protegida contra roubo (Priority: P1)

O dono quer que, mesmo que algum script malicioso chegue a rodar na página (por exemplo, por uma
extensão ou falha futura), ele não consiga copiar a sessão de longa duração do usuário para usá-la em
outro lugar.

**Why this priority**: lembrar o login por 30 dias aumenta o valor de uma sessão roubada; a proteção
precisa acompanhar a mudança.

**Independent Test**: logado, inspecionar o que a página consegue ler (armazenamento e cookies
acessíveis por script) → a sessão de longa duração não aparece em lugar nenhum legível pela página.

**Acceptance Scenarios**:

1. **Given** um usuário logado, **When** um script da página tenta ler a sessão de longa duração,
   **Then** não consegue: ela só é enviada pelo navegador ao próprio servidor do sistema.
2. **Given** um outro site aberto no mesmo navegador, **When** ele tenta usar a sessão do sistema,
   **Then** o navegador não a envia (a sessão só vale para o próprio sistema).
3. **Given** o sistema em produção, **When** a sessão é guardada, **Then** ela só trafega em conexão
   segura (HTTPS).

---

### User Story 3 - Sair encerra a sessão de verdade (Priority: P2)

Ao clicar em **Sair**, a sessão é apagada do navegador e invalidada no servidor: reabrir o sistema, em
qualquer aba, pede e-mail e senha.

**Why this priority**: com o login lembrado, sair é a forma de proteger um computador compartilhado.

**Independent Test**: logado em duas abas, sair numa → as duas passam a exigir login.

**Acceptance Scenarios**:

1. **Given** um usuário logado, **When** clica em Sair e reabre o sistema, **Then** a tela de login aparece.
2. **Given** o sistema aberto em duas abas, **When** o usuário sai em uma, **Then** a outra deixa de estar
   logada na próxima ação ou navegação.
3. **Given** a sessão vencida ou revogada no servidor, **When** o usuário usa o sistema, **Then** vai para a
   tela de login e a sessão guardada é descartada.

---

### Edge Cases

- **Atualização com usuários logados**: quem estiver logado no momento da publicação precisa entrar uma
  única vez (a sessão antiga não é migrada, pois ficava legível pela página); depois disso fica lembrado.
- **Acesso sem HTTPS** (pelo IP da VPS, antes do domínio): o sistema continua funcionando; o login só
  passa a ser lembrado entre aberturas quando o acesso for pelo domínio com HTTPS.
- **Sessão vencida (mais de 30 dias) ou revogada**: tela de login sem mensagem técnica.
- **Modo anônimo**: a sessão some ao fechar a janela anônima, sem erro.
- **Cookies bloqueados pelo navegador**: o sistema funciona, mas pede login a cada abertura.
- **Usuário A sai e usuário B entra no mesmo navegador**: nada do A permanece.
- **Desenvolvimento local** (sistema e API em portas diferentes da mesma máquina): o login lembrado
  também funciona.
- **Sistema antigo**, que usa o mesmo banco: continua funcionando como hoje, sem impacto.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST manter o usuário logado no mesmo navegador após fechar a aba, o navegador ou
  reiniciar o computador, enquanto a sessão for válida no servidor.
- **FR-002**: Novas abas do sistema no mesmo navegador MUST abrir já logadas.
- **FR-003**: A sessão de longa duração MUST ser guardada pelo servidor num cookie que scripts da página
  não conseguem ler, enviado só ao próprio sistema e, em produção, só por conexão segura.
- **FR-004**: A sessão de longa duração MUST NOT ser entregue à página nem guardada em nenhum
  armazenamento do navegador acessível por script.
- **FR-005**: O prazo máximo do login lembrado MUST ser o prazo de sessão já definido pelo servidor
  (30 dias desde o login), com renovação automática do acesso de curta duração (1 hora) sem senha.
- **FR-006**: Ao clicar em **Sair**, o sistema MUST invalidar a sessão no servidor e apagar o cookie; as
  demais abas MUST deixar de estar logadas.
- **FR-007**: Quando a sessão for recusada (vencida ou revogada), o sistema MUST apagar o cookie e mostrar
  a tela de login.
- **FR-008**: A proteção do cookie MUST impedir que outros sites usem a sessão do sistema em nome do usuário.
- **FR-009**: Sem HTTPS (acesso pelo IP) ou com cookies bloqueados, o sistema MUST continuar funcionando,
  apenas sem lembrar o login entre aberturas.
- **FR-010**: Mensagens ao usuário MUST estar em português.

### Key Entities

- **Sessão de longa duração**: credencial de renovação válida por até 30 dias, guardada em cookie
  protegido, revogável pelo servidor (sair, vencimento).
- **Acesso de curta duração**: credencial de 1 hora usada nas operações, mantida só enquanto a aba está
  aberta e renovada a partir da sessão.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das reaberturas do sistema pelo domínio, dentro do prazo da sessão, abrem logadas sem
  digitar senha.
- **SC-002**: O atendente digita a senha no máximo 1 vez a cada 30 dias por navegador (salvo ao sair).
- **SC-003**: Em 0% dos casos a sessão de longa duração fica visível para scripts da página ou é enviada a
  outros sites.
- **SC-004**: Após Sair, 100% das reaberturas e das outras abas exigem login.
- **SC-005**: Nenhuma operação do sistema deixa de funcionar com a mudança (cadastros, OS, PDFs, tempo real).

## Assumptions

- O sistema em produção será acessado pelo domínio com HTTPS (`gerenciamento.tvseletronica.com`); pelo IP
  sem HTTPS, o login não é lembrado entre aberturas.
- O prazo de 30 dias e a renovação a cada hora já existentes no servidor são mantidos.
- O sistema web e a API ficam no mesmo endereço em produção (a API em `/api`), então o cookie é do
  próprio site.
- O site público não usa login e não é afetado.
- Não haverá opção "lembrar de mim": lembrar é o padrão; em computador compartilhado, usar **Sair**.
- O sistema antigo continua funcionando com o mesmo banco, sem mudanças.

# Contract: estrutura do README.md

Seções obrigatórias, nesta ordem (títulos em pt-BR, âncoras geradas pelo GitHub):

1. **TVS-App** — uma frase do que é + selos opcionais.
2. **Índice** — links para todas as seções de nível 2 e para cada grupo de rotas.
3. **Sobre o projeto** — para quem é, o que resolve, funcionalidades principais.
4. **Tecnologias** — backend, frontend, banco, tempo real, mensageria, infraestrutura, observabilidade.
5. **Arquitetura** — diagrama; camadas `Domain → Application → Infrastructure → Api`; web; Caddy; SignalR; fila do WhatsApp.
6. **Estrutura de pastas** — árvore com o papel de cada pasta.
7. **Como rodar localmente** — pré-requisitos; configuração; banco local; API; web; Swagger.
8. **Testes** — comando e cobertura.
9. **API** — convenções (base `/api` atrás do Caddy, camelCase, enums numéricos, envelope, erros, autenticação, correlação), depois um subtítulo por grupo:
   Autenticação · Clientes e endereço · Ordens de serviço (consultas) · Ordens de serviço (comandos) · Pagamentos · Métricas do dashboard · Rotas públicas · Tempo real (`/osHub`) · Rotas internas.
10. **Modelos de dados** — objetos de entrada, DTOs de saída, enums.
11. **Notificações de WhatsApp** — mensagens, gatilhos, como conferir envio.
12. **Docker e produção** — serviços, comandos, variáveis, link `DEPLOY.md`.
13. **Observabilidade** — componentes, acesso, painéis, alertas, retenção, correlação, achar OS, eventos, métricas, privacidade.
14. **Contribuindo** — princípios, migrations, testes, pt-BR, fluxo Spec Kit (`specs/`).
15. **Licença** — referência ao `LICENSE`.

## Formato de rota

Tabela-resumo por grupo:

| Método | Rota | Login | Descrição |
|--------|------|-------|-----------|

Bloco por rota (quando há parâmetros/corpo/resposta relevantes):

````markdown
#### `PUT /service-orders/{id}/delivery`
Entrega a OS. **Login**: sim. **Evento**: `service_order.delivered`.
- Rota: `id` (número da OS)
- Corpo: —
- Resposta: `200 application/pdf` (comprovante) ou envelope de erro
````

## Formato de objeto/DTO

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|

seguido de exemplo JSON em camelCase com enums numéricos.

## Regras

- Toda rota do código aparece exatamente uma vez nas tabelas-resumo.
- Nenhum segredo real.
- Comandos em blocos `bash` copiáveis.

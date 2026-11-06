# Contract: comando de verificação de fluxo

## Execução

```bash
dotnet test src/TVSApp.IntegrationTests --logger "console;verbosity=normal"
```

Pré-requisito: Docker em execução. Não usa `.env`, Azure SQL nem Evolution reais.

## Saída

- Uma linha por cenário: `Aprovado <nome do cenário>` / `Com falha <nome do cenário>`.
- Em falha: `Etapa <etapa>: esperado <…> / obtido <…>`.
- Total ao final (`Aprovado! – Com falha: 0, Aprovado: N …`). Código de saída ≠ 0 se algo falhar.

## Cenários mínimos (nomes em pt-BR)

1. Login com usuário inicial devolve tokens; senha errada é recusada.
2. Abrir OS gera PDF, `X-Service-Order-Id` e WhatsApp de boas-vindas ao cliente.
3. Orçamento envia WhatsApp com número da OS e código de segurança.
4. Aprovação pela loja envia agradecimento ao cliente e aviso ao dono.
5. Aprovação pelo cliente (link público com código) tem o mesmo efeito; código errado → 404 e nenhuma mensagem.
6. Reprovação envia confirmação ao cliente e aviso ao dono; liberar reprovado envia aviso de retirada.
7. Philco/Britania: orçamento e aprovação **não** enviam WhatsApp ao cliente.
8. Compra de peça e conserto: conserto envia "aparelho pronto".
9. Entrega de OS aprovada com conserto devolve PDF e envia garantia.
10. Pagamentos: adicionar, alterar, listar, comprovante em PDF, remover.
11. Venda gera PDF e envia WhatsApp de venda.
12. Contato do site enfileira e envia ao dono; busca e métricas respondem com os dados criados.

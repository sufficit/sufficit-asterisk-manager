# Dependência explícita de JSON para publicação dos recebimentos

O publish Release do Blazor falhava com CS0234 porque ActionDispatcher, ManagerEventBuilder e ManagerResponseBuilder usam Sufficit.Json sem referência direta. Declarado ProjectReference quando o repositório irmão existe, com fallback NuGet `Sufficit.Json` 1.*, seguindo a convenção do projeto.

Commit `48a1638` enviado para main. A alteração ficou restrita ao csproj. Publish Release do Blazor aprovado após a correção e publicação final verificada em produção. Nenhuma alteração de comportamento AMI.

O workflow de packages está ativo, mas a API não apresentou execução nova associada durante a entrega; não houve alegação de publicação de um novo pacote NuGet. A validação foi pelo grafo real do Blazor, que faz checkout deste repositório.

[Relatório integrado](../../../sufficit-blazor/docs/activities/202609211228-recebimentos-recentes.md). Encerra `docs/PLAN-RECEBIMENTOS-DEPENDENCIA-JSON.md`.

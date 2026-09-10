# Recepção limitada — publicação isolada nos canários

Limites opt-in de Core/Shared/Manager publicados em 2026-09-09 apenas como dependências
do novo `Sufficit.AMIEvents.Observation` no eveo-apps. Apoint/Eveo usam suas instâncias
principais de Asterisk; não há PBX adicional. O serviço legado que consome Google
permaneceu com PID/configuração anteriores e sem atualização dos binários.

16 verificações de regressão do Manager e 19 do host de observação passaram.
Contratos dos limites em `202609091825-bounded-ami-receive.md` continuam válidos.
Detalhes operacionais, testes e falha inicial de permissão corrigida estão em
`sufficit-ami-events/docs/activities/202609091848-ami-observation-rollout.md`.
O plano temporário foi encerrado; reconciliação de chamadas e migração de consumidores
de negócio continuam incrementos separados, não funcionalidades entregues aqui.

# Recepção AMI limitada — 2026-09-09 18:25 -03

Implementação local opt-in; não publicada no AMIEvents em execução.

## Contrato
Configurar conjuntamente os quatro campos herdados de ManagerConnectionParameters:

```json
{
  "ReceivePacketCapacity": 128,
  "ReceiveLineCapacity": 1024,
  "ReceiveMaxLineChars": 4096,
  "ReceiveMaxPacketChars": 65536
}
```

Esses valores são ponto inicial de ensaio, não dimensionamento homologado para 500
ligações. Todos zero preservam comportamento legado; configuração parcial/inválida
é recusada. Limites ficam fixos pela vida da conexão: não mutar parâmetros em uso.
Contagem é em caracteres decodificados e quantidade de itens, não estimativa exata de heap.

Limites atuam em Shared (linha completa/parcial e fila de linhas), Manager (montagem
do pacote inclusive Response:Follows e fila de pacotes) e assinatura interna estrita.
O buffer de socket ainda limita bytes lidos por iteração. Não são limites de todo o
processo: caches/fila de escrita/ações criadas pelo consumidor precisam de seus limites.

`ManagerEventSubscriptions(capacity, rejectWhenFull: true)` admite via TryDispatch,
não espera consumidor e não remove evento antigo para aparentar sucesso. Conexão
limitada recusa assinaturas externas legadas; default público anterior permanece
DropOldest para compatibilidade, sem garantia de histórico confiável.

Ao exceder limite, fecha socket e sinaliza ReceiveLimitExceeded. Respostas pendentes
falham com NotConnectedException; resultado de comando já enviado é desconhecido,
não repetir comandos mutantes automaticamente. Frames pendentes da instância encerrada
não são redistribuídos. Um callback já em execução não pode ser desfeito.

Em modo limitado, qualquer desconexão encerra a geração (`RequiresReplacement`).
O reconnector da instância não reutiliza filas antigas: o AsteriskManagerProvider
substitui a instância no próximo ConnectAsync. Supervisão existente controla retry/backoff.
Também há rechecagem de conexão depois da assinatura de OnDisconnected para não
esperar eternamente um evento ocorrido entre login e instalação do observador.
Isso não reconstrói eventos perdidos: consumidor deve reconciliar seu estado e marcar lacuna.

## Testes
`dotnet run --project tests/Sufficit.Asterisk.Manager.ReceiveTests.csproj
-p:BuildInParallel=false --no-launch-profile`: 16 PASS com TCP simulado.
Login, resposta correlacionada, fragmentação, linha sem terminador grande, Command grande,
fila de linhas cheia, consumidor travado, resposta pendente falha, rejeição de assinatura
com perdas, descarte explícito da geração encerrada e provider com nova conexão.
Build Manager net10/net7/netstandard2.0 PASS. Não são testes de carga de produção.

## Impedimento de publicação central
AMIEvents usa singleton externo legado e funções de notificação de negócios. Necessita
fluxo de observação separado ou migração do fluxo inteiro. ActionGetVar escolhe primeiro
provider e cache de chamadas não possui chave composta por nó. Não basta adicionar
IPs ao JSON. Google e todos os serviços remotos foram preservados nesta etapa.

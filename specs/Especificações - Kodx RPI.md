Data: 16/07/2026
Autor: Marcos Herminio

## Resumo
Sistema baseado em API para gerenciamento de RPIs afim de servir o sistema Kodx. Esta API é responsável por baixar RPIs da fonte (INPI), validar a consistência desses arquivos, convertê-los em txt e salvá-los nos buckets do Kodx, tornando assim acessível para o sistema. Também fará o recorte das publicações individuais brutas, persistindo essas em banco de dados, servindo essas para que outros serviços possam filtrá-las e guardar em outros banco de dados dos clientes.

## Tecnologias
Este serviço será construindo usando .NET core. Contará com sistema de buckets servido pelo Azure para armazenamento das RPIs no formato pdf e txt. O banco de dados será um Postgres mantido no linode, numa instância acessível somente pelos serviços que estarão lá deployados. 
Cada instância contará com os deploys das aplicações em docker, deverão ser versionados usando versionamento semântico. 

## Arquitetura de Alto Nível

Diagrama I - Arquitetura
![[Diagrama1.png.png]]

RPI Service tem dependência do site do INPI, de onde vai fazer o download dos arquivos pdf. Também depende do Azure Blob Storage, onde armazena e busca RPIs salvas com extensão pdf - formato original baixado do INPI - e extensão txt transformado pelo serviço neste formato afim de facilitar a leitura. Além disso, disso depende de um banco de dados no qual salva as publicações individualizadas das RPIs e armazena histórico dos downloads e transformações (tentativa com sucesso e as falhas). Portanto, no banco de dados será possível saber quais RPIs e em quais formatos estão disponíveis e quais estão com problemas.

RPI Service terá suas rotinas de downloads acionadas por um worker interno da própria aplicação (`BackgroundService` em loop) — decisão tomada na fase 9 do plano, substituindo a ideia original de scripts de cronjob externos rodando na instância Ubuntu. O worker verifica periodicamente, usando o calendário oficial do INPI, se há uma edição nova pra processar e, se houver, dispara o pipeline chamando os use cases diretamente (sem depender de rede/gRPC pra si mesmo). Ver `ai/context.md` pra decisões e arquitetura.

O RPI Service servirá endpoints privados para o kodx api para passar informações da RPI, fazer downloads dos PDFs ou pesquisar publicações de edição especifica das RPIs. 

## Especificações da Aplicação

Será usado .NET na sua ultima versão LTS no momento da escrita deste documento. Seguiremos principios de DDD na segregação de responsabilidades e desenho das camadas. Camadas de serviço e que comunicam com as dependências da aplicação terão testes de unidade. 

Antes de cada push para o repositório quero fazer testes de integração localmente para garantir que todos os comportamentos mais importantes estejam funcionando. 

Usaremos como CI o GithubActions que contará com etapa de sec, rodada de testes de unidade, build em todos os fluxos e deploy somente nos casos de versão fechada. 

A aplicação terá logs estruturados que deverão mapear o recebimento de uma request e sua saida como info se tudo correr com sucesso. Em caso de alteração de recurso (create, update, delete, etc) deverá registrar um log especifico para tanto. Em caso de log relevante para a regra de negócio outro log info. Se houver erro em qualquer camada, log de erro. 

O acionamento periódico das rotinas de download é feito por um worker interno da aplicação (`BackgroundService`), não por cronjobs do sistema operacional — ver decisão acima e em `ai/context.md`, fase 9.

Os endpoints privados (item acima) são servidos via gRPC, não REST — decisão tomada na fase 8 do plano. Documentação/exploração via server reflection (`Grpc.AspNetCore.Server.Reflection`) + coleção Bruno versionada em `docs/Kodx API/` (Bruno tem suporte a gRPC, incluindo reflection e streaming, desde a v2.10) no lugar de Swagger. Endpoints precisam de chave de API (mesma convenção de header, agora lida via metadata gRPC) e deverão ter configuração de timeout default ou especifica caso necessário.

Para o desenvolvimento usaremos localmente o direnv, com arquivo local .envrc, não versionado. 

## Especificações das dependências

### Banco de Dados
Será usado um banco de dados postgres. A premissa é o uso relacional. A excessão será uso de base não relacional para guardar as publicações individualizadas em jsonb.

### Blob Storage
Bucket que já guarda hoje as RPIs no formato pdf e txt. Seguiremos o formato já existente de segregação e tags. Essa estrutura roda no serviço de nuvem do Azure e deve ter uma chave especifica para ser usada pelo servico. 

### INPI
Site do INPI de onde são baixadas as RPIs. 
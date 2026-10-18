# NewLife.Core - Biblioteca Principal

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-29%2F29-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm32%20%7C%20LoongArch64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

O framework NewLife é um ecossistema abrangente de componentes .NET, que oferece infraestrutura de alto desempenho para a construção de aplicações escaláveis. Ele fornece funcionalidades básicas como log, rede, serialização, cache e multithreading, servindo de base para uma série de aplicações, de serviços Web a dispositivos IoT.  

Tutorial da biblioteca principal: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ Esta tradução pode ficar desatualizada; a versão autorizada é o [README em chinês](Readme.MD).


**Conteúdo**: [Visão geral do framework](#visão-geral-do-framework) · [Por que NewLife.Core](#por-que-newlifecore) · [Início rápido](#início-rápido) · [Principais funcionalidades](#principais-funcionalidades) · [Índice de documentação](#índice-de-documentação) · [Estrutura do repositório](#estrutura-do-repositório) · [Compilar e testar](#compilar-e-testar) · [Matriz de projetos NewLife](#matriz-de-projetos-newlife) · [Contribuição e feedback](#contribuição-e-feedback) · [Licença](#licença)

## Visão geral do framework

O NewLife é composto por várias bibliotecas de componentes, e o NewLife.Core fornece a base. O framework suporta múltiplos frameworks de destino, de net45 a net10.0 (.NET Framework 4.5 até .NET 10.0), proporcionando ampla compatibilidade em diferentes ambientes.

**NativeAOT**: o caminho principal já é suportado (29 cenários testados + CI contra regressões); capacidades dinâmicas como plugins e scripts não são suportadas por design; consulte os limites em [Análise de compatibilidade com AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) (中文).


## Por que NewLife.Core

- **Uma biblioteca para a infraestrutura comum**: log, configuração, cache, rede, serialização, agendamento, contêiner de objetos e DI — basta uma referência.
- **Ampla cobertura de versões**: 11 frameworks de destino em um único pacote (net45 → net10.0, incluindo variantes do Windows); a mesma API para sistemas legados e projetos novos.
- **Pronto para NativeAOT**: 29 cenários do núcleo medidos e protegidos por uma matriz de CI multi-RID; limites na [análise de compatibilidade AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md).
- **Dependências contidas e licença permissiva**: apenas a BCL e pacotes de compatibilidade da Microsoft; licença MIT, permitindo modificação e rebranding (veja [Licença](#licença)).

## Início rápido

### Instalação

```bash
dotnet add package NewLife.Core
```

### Exemplo mínimo

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // log no console (também grava em arquivo)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // cache em memória
cache.Set("key", "value", 60);          // expira em 60 segundos
var value = cache.Get<String>("key");   // leitura
```

### Compilar e personalizar

Os componentes NewLife são criados em conjunto pela comunidade há mais de 20 anos e adotam a licença open source MIT; **qualquer pessoa pode modificá-los e redistribuí-los livremente** (sem necessidade de declarar a origem)! Muitas empresas, ao construir frameworks internos de desenvolvimento com base neles, podem até realizar personalização privada substituindo em massa todas as strings `NewLife` no código-fonte pelo nome da sua empresa.

A equipe sempre mantém uma postura aberta: além de suportar a compilação ao abrir a solução no VisualStudio (versão estável mais recente), também é compatível com a compilação pela linha de comando `dotnet build`. Os arquivos de projeto abandonam funcionalidades complexas em busca de simplicidade e facilidade de uso, sendo realmente prontos para usar desde o primeiro momento.

Divulgamos publicamente o certificado de nome forte `newlife.snk` para permitir compilar e substituir os assemblies por conta própria.

### Modelo de projeto

Execute o seguinte comando na linha de comando para experimentar rapidamente os componentes NewLife:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

## Índice de documentação

### Documentação de uso

- [Catálogo da biblioteca principal](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md) (中文): índice de artigos dos componentes principais
- [Análise de compatibilidade com AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) (中文): escopo de suporte do NativeAOT, conclusões de testes reais (smoke 29/29), guia de uso e limites conhecidos

### Documentação de desenvolvimento

O diretório `Doc/` do repositório mantém o sistema de documentação de capacidades (definição de requisitos → rastreamento de funcionalidades → design de arquitetura):

- [Documento de requisitos](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md) (中文): visão, objetivos centrais e requisitos funcionais
- [Lista de funcionalidades](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md) (中文): rastreamento do status de implementação/teste/comentários de todos os pontos de funcionalidade
- [Design de arquitetura](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md) (中文): camadas, componentes de módulos e decisões-chave de design

## Principais funcionalidades


<details>
<summary><b>Extensões básicas</b> (9)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Conversão de tipos Utility](https://newlifex.com/core/utility) | Fornece conversão de tipos eficiente, com suporte a vários formatos e tratamento de exceções. |
| [Extensão de strings StringHelper](https://newlifex.com/core/string_helper) | Inclui diversas operações de string, como corte, codificação e formatação. |
| [Extensão de caminhos PathHelper](https://newlifex.com/core/path_helper) | Trata de forma inteligente vários formatos de caminho, garantindo compatibilidade do código entre plataformas. |
| [Extensão de dados IOHelper](https://newlifex.com/core/io_helper) | Otimiza operações de IO, oferecendo conversão eficiente entre fluxos de dados e arrays de bytes. |
| [Extensão de processos ProcessHelper](https://newlifex.com/core/process_helper) | Simplifica operações com processos, oferecendo métodos práticos para obter e gerenciar informações de processos. |
| [Informações de runtime Runtime](https://newlifex.com/core/runtime) | Oferece métodos simples para obter informações de runtime da aplicação, facilitando depuração e diagnóstico. |
| [Descartável DisposeBase](https://newlifex.com/core/disposebase) | Fornece um padrão padronizado de liberação de recursos, prevenindo efetivamente vazamentos de memória e de recursos. |
| [Extensão de reflexão Reflect](https://newlifex.com/core/reflect) | Ferramenta de reflexão de alto desempenho, para leitura e escrita rápidas de membros de objetos. |
| [Extensão de segurança SecurityHelper](https://newlifex.com/core/security_helper) | Integra RSA/AES/DES/MD5 e diversos outros algoritmos de criptografia e descriptografia, simplificando operações de segurança. |

</details>

<details>
<summary><b>Log e rastreamento</b> (2)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Log ILog](https://newlifex.com/core/log) | Suporta várias formas de saída de log e oferece poderosa gravação assíncrona. |
| [Rastreamento distribuído ITracer](https://newlifex.com/core/tracer) | Poderoso rastreamento de desempenho APM, com suporte à integração com a plataforma Stardust. |

</details>

<details>
<summary><b>Agendamento</b> (3)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Temporizador avançado TimerX](https://newlifex.com/core/timerx) | Temporizador de alta precisão, com suporte a agendamento Cron e execução periódica assíncrona. |
| [Expressão Cron](https://newlifex.com/core/cron) | Oferece tarefas agendadas semelhantes ao Cron do Linux, com correspondência no nível de segundos. |
| [Provedor de tempo TimeProvider](https://newlifex.com/core/timerx_timeprovider) | Abstração de tempo unificada e cálculo de duração de alta precisão, facilitando a compatibilidade entre versões. |

</details>

<details>
<summary><b>Sistema de configuração</b> (4)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Sistema de configuração Config](https://newlifex.com/core/config) | Framework de configuração unificado, com suporte a múltiplos formatos, múltiplas fontes e mecanismo de recarga a quente. |
| [Provedor de configuração IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | Interface de configuração unificada, com suporte a callbacks e ao modelo de nós de configuração. |
| [Configuração central Setting](https://newlifex.com/core/setting) | Gerencia as configurações centrais dos componentes, controlando comportamentos básicos como log e rede. |
| [Configuração de sistema SysConfig](https://newlifex.com/core/sysconfig) | Gerencia as configurações do sistema da aplicação, com suporte a configurações locais, distribuídas e criptografadas. |

</details>

<details>
<summary><b>Serialização</b> (7)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Serialização Json](https://newlifex.com/core/json) | Ferramenta Json de alto desempenho, com suporte a serialização em diversos cenários complexos. |
| [Serialização Xml](https://newlifex.com/core/xml) | Oferece serialização e desserialização XML, com conversão bidirecional entre objetos e XML. |
| [Serialização binária](https://newlifex.com/core/binary) | Serialização binária de alto desempenho, com suporte a grafos de objetos complexos e controle de versão. |
| [Serialização binária avançada](https://newlifex.com/core/advanced_binary) | Guia avançado de codificação/decodificação com zero alocações em todo o fluxo de mensagens de protocolo. |
| [Análise de Csv CsvFile](https://newlifex.com/core/csv_file) | Leitura e escrita rápidas de arquivos CSV, com suporte a delimitadores personalizados e vinculação de dados. |
| [Banco de dados Csv CsvDb](https://newlifex.com/core/csv_db) | Usa arquivos CSV como banco de dados, com suporte a operações básicas de CRUD. |
| [Leitor leve de Excel ExcelReader](https://newlifex.com/core/excel_reader) | Sem dependência do Office, lê Excel rapidamente, com suporte a grandes volumes de dados. |

</details>

<details>
<summary><b>Cache</b> (7)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Interface de cache unificada ICache](https://newlifex.com/core/icache) | Define operações padrão de cache e oferece diversas implementações, como memória e Redis. |
| [Arquitetura de cache ICacheProvider](https://newlifex.com/core/icacheprovider) | Usada para gerenciar e fornecer instâncias de ICache, com suporte à coexistência de múltiplos caches. |
| [Cache em memória MemoryCache](https://newlifex.com/core/memory_cache) | Cache em memória de máquina única e alto desempenho, com suporte a políticas de expiração e capacidade. |
| [Pool de objetos ObjectPool](https://newlifex.com/core/object_pool) | Reutiliza objetos custosos, melhorando o desempenho e reduzindo a pressão sobre o GC. |
| [Pool de objetos Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | Pool de objetos genérico de alto desempenho baseado em CAS, que reduz a pressão sobre o GC. |
| [Cache distribuído NewLife.Redis](https://newlifex.com/core/redis) | Implementação de cache distribuído baseada em Redis, com suporte a diversas estruturas de dados. |
| [Armazenamento vetorial IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | Interface de gerenciamento de coleções de vetores, com suporte a busca de similaridade Top-K, fornecendo infraestrutura de busca vetorial para aplicações de IA. |

</details>

<details>
<summary><b>Buffers de alto desempenho</b> (7)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Pacote de dados IPacket](https://newlifex.com/core/packet) | Interface central de pacote de dados, com fatias zero-copy e pacotes em cadeia. |
| [Buffers](https://newlifex.com/core/buffers) | Biblioteca de operações de buffer de alto desempenho, que reúne componentes como Span e escrita em pool. |
| [Escritor em pool PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | Escritor de buffer em pool baseado em ArrayPool, com suporte a expansão dinâmica. |
| [Leitor Span SpanReader](https://newlifex.com/core/span_reader) | Analisador binário de alto desempenho com zero alocação, com suporte a big-endian e little-endian. |
| [Escritor Span SpanWriter](https://newlifex.com/core/span_writer) | Escritor de dados binários de alto desempenho com zero alocação, com suporte a big-endian e little-endian. |
| [Auxiliar de Span SpanHelper](https://newlifex.com/core/span_helper) | Conjunto de métodos de extensão para Span/ReadOnlySpan, simplificando operações de memória. |
| [Leitor sequencial SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Leitor sequencial entre segmentos (em frameworks antigos, suprido por shim; disponível em todos os 11 TFMs). |

</details>

<details>
<summary><b>Framework de aplicação</b> (16)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Contêiner de objetos ObjectContainer](https://newlifex.com/core/object_container) | Injeção de dependência leve, com suporte a vários ciclos de vida e modos de injeção. |
| [Host de aplicação leve Host](https://newlifex.com/core/host) | Simplifica a hospedagem de aplicações, facilitando a construção de serviços em segundo plano de longa duração. |
| [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Oferece mecanismo padronizado de gerenciamento de plugins, viabilizando o desenvolvimento modular de aplicações. |
| [Modelo de concorrência Actor](https://newlifex.com/core/actor) | Modelo simplificado de programação concorrente, que melhora a vazão por meio do processamento em fila de mensagens. |
| [Modelo de pipeline Pipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) ⚠️ Descontinuado, substituído pelo modelo de protocolo (frame pump) | Modelo de cadeia de processamento bidirecional, que unifica os fluxos Read/Write/Open/Close. |
| [Fila de atraso DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Agrega alterações de alta frequência e as persiste em lote, reduzindo a amplificação de escrita. |
| [Barramento de eventos EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Distribuição de eventos em processo, com suporte a roteamento por tópico e mensagens em fila. |
| [Delegado de referência fraca WeakAction](https://newlifex.com/core/weak_action) | Resolve vazamentos de memória em assinaturas de eventos, implementando tratamento de eventos seguro. |
| [Mecanismo de script ScriptEngine](https://newlifex.com/core/script_engine) | Suporta compilação e execução dinâmica de C#, oferecendo capacidade flexível de extensão por scripts. |
| [Compressão e descompressão](https://newlifex.com/core/compression) | Integra diversos algoritmos de compressão e fornece interface unificada para compressão de dados. |
| [Arquivamento comprimido Tar e 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Cobre usos avançados de arquivamento e compressão com Tar/Tar.Gz e 7Zip. |
| [Informações da máquina MachineInfo](https://newlifex.com/core/machine_info) | Obtém informações de hardware de forma abrangente, com suporte a sistemas Windows e Linux. |
| [Biblioteca de pinyin PinYin](https://newlifex.com/core/pinyin) | Biblioteca eficiente para converter caracteres chineses em pinyin, com suporte a pinyin completo e abreviado e desempenho superior. |
| [Conjunto de dados DbTable](https://newlifex.com/core/dbtable) | Tabela de dados em memória, que facilita o processamento e a análise de dados. |
| [Parâmetro de paginação PageParameter](https://newlifex.com/core/page_parameter) | Encapsula a lógica de paginação, com suporte a paginação em diversos cenários, como Web e API. |
| [Algoritmo Snowflake](https://newlifex.com/core/snow_flake) | Algoritmo distribuído de geração de IDs únicos, que garante a unicidade dos IDs sob alta concorrência. |

</details>

<details>
<summary><b>Segurança e tokens</b> (3)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Token de assinatura digital distribuída TokenProvider](https://newlifex.com/core/token_provider) | Usado para gerar e validar tokens distribuídos, garantindo segurança entre múltiplos nós. |
| [Token Web universal JwtBuilder](https://newlifex.com/core/jwt) | Oferece geração, decodificação e validação de tokens JWT, simplificando a autenticação Web. |
| [Filtro HTTP de token TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Injeta automaticamente o token nas requisições HTTP e renova automaticamente ao interceptar 401/403. |

</details>

<details>
<summary><b>Serviços em nuvem</b> (1)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Hash de latitude e longitude GeoHash](https://newlifex.com/core/geo_hash) | Codifica latitude e longitude como strings, para uso em busca de locais próximos. |

</details>

<details>
<summary><b>Biblioteca de rede</b> (15)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Servidor de rede NetServer](https://newlifex.com/core/netserver) | Servidor TCP/UDP/Unix domain socket de alto desempenho, que encapsula sessões e processamento de dados. |
| [Cliente de rede NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Cliente de rede TCP/UDP/Unix unificado, que simplifica a conexão e o envio/recebimento. |
| [Mensagem IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Contrato de mensagem de requisição-resposta, que carrega semântica de direção e leitura/escrita de frames. |
| [Pilha de protocolo de mensagens MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Pilha única de frame pump + codec sem estado, com suporte a corpo de frames grandes em streaming e backpressure. |
| [Pipeline de dados Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Pipeline de dados com zero cópia, alinhado ao formato de System.IO.Pipelines. |
| [Protocolo simples de troca de mensagens remotas SRMP](https://newlifex.com/core/srmp) | Protocolo RPC leve, com suporte a serialização binária e transporte UDP. |
| [Balanceamento de carga e failover LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Oferece três estratégias de escalonamento entre múltiplos nós: failover, round-robin e corrida (racing). |
| [Comunicação RPC ApiServer](https://newlifex.com/core/api_server) | ⚠️ Movido para [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [Comunicação RPC ApiClient](https://newlifex.com/core/api_client) | ⚠️ Movido para [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [Cliente HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Encapsulamento baseado em HttpClient, que simplifica a chamada e o tratamento de Web APIs. |
| [Servidor HTTP HttpServer (dispositivos embarcados)](https://newlifex.com/core/httpserver) | Servidor Http leve, com suporte a pipeline de middleware, rotas parametrizadas e serviço de arquivos estáticos. |
| [Download de rede WebClientX](https://newlifex.com/core/webclient) | Versão aprimorada do WebClient, com suporte a download multithread e retomada de download. |
| [Servidor WebSocket (ferramenta definitiva de notificação downstream para IoT)](https://newlifex.com/core/websocket) | Servidor WebSocket de alto desempenho, adequado para push de mensagens da Internet das Coisas. |
| [Comunicação bidirecional WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Apresenta práticas de handshake, processamento de frames, heartbeat e fechamento de conexão. |
| [Cliente de armazenamento de arquivos EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | Cliente HTTP de armazenamento de arquivos, integrado ao EasyIO ou OSS. |

</details>

> ⚠️ Mapas Baidu/Amap foram extraídos para pacotes NuGet independentes.


## Estrutura do repositório

| Diretório | Descrição |
| --- | --- |
| `NewLife.Core/` | Código da biblioteca principal (log, rede, cache, serialização, …) |
| `NewLife.Core.SourceGenerator/` | Gerador de código distribuído como `analyzers/` no pacote |
| `Doc/` | Documentação de recursos e índice de artigos |
| `Samples/` | Exemplos de console / serviço / Http / IoT / móvel |
| `Benchmark/` | Benchmarks de desempenho com BenchmarkDotNet |
| `XUnitTest.Core/` | Testes unitários principais (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Testes de compatibilidade entre versões |
| `AotSmoke/` | Projeto de smoke test NativeAOT |

## Compilar e testar

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Execução completa em paralelo: `run-tests.ps1` (vários hosts em paralelo + timeout + resumo trx)
- Smoke test de NativeAOT: `AotSmoke/`, CI em `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

## Matriz de projetos NewLife

Todos os projetos suportam por padrão net10.0/netstandard2.1/netstandard2.0/net462/net4.5; as versões antigas (2024.0801) suportam net4.0/net2.0  

| Projeto | Ano | Descrição |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
| Componentes básicos |  | Dão suporte a outros middlewares e a projetos de produto |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Biblioteca principal: log, configuração, cache, rede, serialização e rastreamento de desempenho APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Middleware de big data, tabela única com dezenas de bilhões de linhas, MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng, particionamento automático de tabelas e separação de leitura e escrita |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Biblioteca de rede: throughput de dezenas de milhões por máquina (22,66 milhões de tps), milhões de conexões por máquina (4 milhões de conexões TCP persistentes) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | Biblioteca de comunicação por protocolo, que fornece framework de comunicação para aplicações CS, com suporte a comunicação Http/RPC, alto throughput e integração fácil e de baixo custo para dispositivos IoT |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Plataforma de desenvolvimento rápido Cube, que integra permissões de usuário, login SSO, servidor OAuth e muito mais; validada em projetos com tabela única de 10 bilhões de linhas |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | Componente de gerenciamento de serviços, que instala a aplicação como daemon do sistema operacional: serviço do Windows e Systemd do Linux |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Scaffold Zero, templates de projeto NewLife.Templates baseados no ecossistema de componentes NewLife: Web, WebApi e Service |
| Middlewares |  | Integração com plataformas de middleware conhecidas |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Cliente Redis, latência em microssegundos, throughput de nível de milhões, rica fila de mensagens; validado em projetos com dezenas de bilhões de dados |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | Cliente RocketMQ totalmente gerenciado, com suporte a Apache RocketMQ e à fila de mensagens do Alibaba Cloud; validado em projetos de escala bilionária |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | Protocolo de mensagens para IoT, MqttClient/MqttServer, com suporte do cliente ao Alibaba Cloud IoT |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | Biblioteca padrão de IoT, que define diversas normas e especificações de protocolos de comunicação no campo da Internet das Coisas |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII, implementado com base na biblioteca padrão de IoT, com suporte à plataforma ZeroIoT e ao gateway IoTEdge |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Protocolo PLC Siemens, implementado com base na biblioteca padrão de IoT, com suporte à plataforma IoT e ao IoTEdge |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | Biblioteca de componentes de mapas, que encapsula Baidu Maps, Amap, Tencent Maps e Tianditu |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | Biblioteca de codecs de áudio, PCM/ADPCMA/G711A/G722U/WAV/AAC |
| Plataformas de produto |  | Nível de plataforma de produto, prontas para compilar, implantar e usar, com personalização individual |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust, plataforma de serviços distribuídos: gerenciamento de nós, central de monitoramento APM, central de configuração, central de registro e central de publicação |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | AntJob, plataforma distribuída de computação de big data (em tempo real/offline), com a filosofia de fragmentação "formigas movendo a casa"; validada em projetos com trilhões de dados |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | ERP empresarial: gerenciamento de produtos, gerenciamento de clientes, gerenciamento de vendas e gerenciamento de fornecedores |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | Ferramenta CrazyCoder, com inúmeras ferramentas para desenvolvedores: rede, porta serial, criptografia e descriptografia, expressões regulares, Modbus e MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | Armazenamento simples de arquivos, com suporte a armazenamento centralizado de arquivos em sistemas distribuídos. |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | Proxy reverso de nível de produto: proxy NAT e proxy Http |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | Ferramenta de teste de estresse Http |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Sistema de gerenciamento de código-fonte Git |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | Sistema operacional embarcado, totalmente independente e autônomo, com suporte à arquitetura de chips ARM Cortex-M |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | Computador industrial embarcado, gateway de borda de Internet das Coisas, host .NET8 de alto desempenho, aplicado em indústria, agricultura, transporte e saúde |
| Plataforma de IoT FIoT | 2020 | Solução completa de Internet das Coisas, para construção, proteção ambiental e agricultura, integrando software, hardware e análise de big data; validada em projetos com centenas de milhares de pontos por máquina |


---

## Equipe de desenvolvimento NewLife

![XCode](https://newlifex.com/logo.png)

A equipe NewLife foi fundada em 2002 e é fornecedora de soluções para o setor de Internet das Coisas da nova era, dedicada a fornecer consultoria de soluções de software e hardware, planejamento de arquitetura de sistemas e serviços de desenvolvimento. O NuGet acumula mais de 5,23 milhões de downloads. Os componentes principais da equipe (XCode / Redis / Stardust / AntJob / IoT, etc.) já são amplamente aplicados em setores como energia elétrica, indústria, transporte, proteção ambiental, saúde, museus e patrimônio cultural.

Melhoramos continuamente e buscamos ser um parceiro de tecnologia .NET / IoT / distribuída digno de confiança de longo prazo.

Site: https://newlifex.com   Organização open source: https://github.com/NewLifeX  
Conta oficial no WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

## Contribuição e feedback

- Antes de enviar, garanta que `dotnet build` e os testes unitários relacionados passem; o estilo de código e as mensagens de commit seguem as convenções do repositório.
- Relate bugs e sugestões via [Issue](https://github.com/NewLifeX/X/issues); Stars e PRs são bem-vindos.
- Consulte [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md) para as notas de versão.

## Licença

Este projeto está licenciado sob [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); o certificado de nome forte `newlife.snk` é público para que você compile e substitua assemblies por conta própria.

> Se este projeto for útil para você, seja bem-vindo a dar uma Star, compartilhar com sua equipe ou enviar sugestões de melhoria por Issue/PR.

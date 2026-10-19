# NewLife.Core - Biblioteca Principal

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
<!-- metrics:start -->
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-31%2F31-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** é a base do ecossistema NewLife: um único pacote para log, configuração, cache, rede, serialização, agendamento, contêiner de objetos e rastreamento APM. Ele suporta 11 frameworks, de net45 a net10.0, e sustenta XCode, Cube, Redis, Stardust e mais de 30 projetos irmãos.

Tutorial da biblioteca principal: [https://newlifex.com/core](https://newlifex.com/core)  

**Conteúdo**: [Início rápido](#início-rápido) · [Por que NewLife.Core](#por-que-newlifecore) · [Principais funcionalidades](#principais-funcionalidades) · [Ecossistema](#ecossistema) · [Documentação](#documentação) · [Contribuição](#contribuição) · [Equipe](#equipe) · [Licença](#licença)

## Início rápido

### Instalação

```bash
dotnet add package NewLife.Core
```

### Exemplo mínimo

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### Modelo de projeto

Execute os comandos abaixo na linha de comando para conhecer rapidamente os componentes NewLife:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### Suporte a plataformas

| Item | Cobertura |
| --- | --- |
| Frameworks de destino | 11 em um único pacote: net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| Sistemas e arquiteturas | Windows, Linux, macOS, Android, iOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> cenários centrais medidos; matriz de smoke de CI com vários RIDs (win-x64, linux-x64, linux-arm64 e linux-loongarch64 são portões bloqueantes). Plugins e scripts não são compatíveis com AOT por design — consulte [Análise de compatibilidade com AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |

## Por que NewLife.Core

- **Um pacote para a infraestrutura comum**: log, configuração, cache, rede, serialização, agendamento, contêiner de objetos e DI — uma única referência basta.
- **Ampla cobertura de versões**: 11 frameworks de destino em um único pacote (net45 → net10.0, incluindo variantes do Windows); a mesma API para sistemas legados e projetos novos.
- **Comprovado na escala do ecossistema**: a mesma base sustenta XCode (tabelas únicas de 10 bilhões de linhas), Net (22,66M tps), Redis e Stardust (dezenas de bilhões de registros).
- **Dependências contidas e licença permissiva**: apenas a BCL e pacotes de compatibilidade da Microsoft; licença MIT, permitindo modificação e rebranding (veja [Licença](#licença)).

## Principais funcionalidades

<details>
<summary><b>Extensões básicas</b> (10)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Conversão de tipos Utility](https://newlifex.com/core/utility) | Fornece conversão de tipos eficiente, com suporte a vários formatos e tratamento de exceções. |
| [Extensão de strings StringHelper](https://newlifex.com/core/string_helper) | Inclui diversas operações de string, como corte, codificação e formatação. |
| [Extensão de caminhos PathHelper](https://newlifex.com/core/path_helper) | Trata de forma inteligente vários formatos de caminho, garantindo compatibilidade do código entre plataformas. |
| [Extensão de dados IOHelper](https://newlifex.com/core/io_helper) | Otimiza operações de IO, proporcionando conversão eficiente entre fluxos de dados e arrays de bytes. |
| [Extensão de processos ProcessHelper](https://newlifex.com/core/process_helper) | Simplifica operações com processos, oferecendo métodos práticos para obter e gerenciar informações de processos. |
| [Informações de runtime Runtime](https://newlifex.com/core/runtime) | Oferece métodos simples para obter informações de runtime da aplicação, facilitando depuração e diagnóstico. |
| [Descarte de recursos DisposeBase](https://newlifex.com/core/disposebase) | Oferece um padrão padrão de liberação de recursos, evitando vazamentos de memória e de recursos. |
| [Extensão de reflexão Reflect](https://newlifex.com/core/reflect) | Ferramenta de reflexão de alto desempenho, para ler e escrever membros de objetos com rapidez. |
| [Extensão de segurança SecurityHelper](https://newlifex.com/core/security_helper) | Integra RSA/AES/DES/MD5 e outros algoritmos de criptografia e descriptografia, simplificando operações de segurança. |
| [Hash de latitude e longitude GeoHash](https://newlifex.com/core/geo_hash) | Codifica latitude e longitude em strings, para busca de locais próximos. |

</details>

<details>
<summary><b>Log e rastreamento</b> (2)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Log ILog](https://newlifex.com/core/log) | Suporta várias formas de saída de log e oferece escrita assíncrona poderosa. |
| [Rastreamento ITracer](https://newlifex.com/core/tracer) | Rastreamento de desempenho APM poderoso, com suporte à integração com a plataforma Stardust. |

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
| [Sistema de configuração Config](https://newlifex.com/core/config) | Framework de configuração unificado, com suporte a múltiplos formatos, múltiplas fontes e recarga a quente. |
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
| [Arquitetura de cache ICacheProvider](https://newlifex.com/core/icacheprovider) | Gerencia e fornece instâncias de ICache, com suporte à coexistência de múltiplos caches. |
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
<summary><b>Framework de aplicação</b> (15)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Contêiner de objetos ObjectContainer](https://newlifex.com/core/object_container) | Injeção de dependência leve, com suporte a vários ciclos de vida e modos de injeção. |
| [Host de aplicação leve Host](https://newlifex.com/core/host) | Simplifica a hospedagem de aplicações, facilitando a construção de serviços em segundo plano de longa duração. |
| [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Oferece mecanismo padronizado de gerenciamento de plugins, viabilizando o desenvolvimento modular de aplicações. |
| [Modelo de concorrência Actor](https://newlifex.com/core/actor) | Modelo simplificado de programação concorrente, que melhora a vazão por meio do processamento em fila de mensagens. |
| [Fila de atraso DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Agrega alterações de alta frequência e as persiste em lote, reduzindo a amplificação de escrita. |
| [Barramento de eventos EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Distribuição de eventos em processo, com suporte a roteamento por tópico e mensagens em fila. |
| [Delegado de referência fraca WeakAction](https://newlifex.com/core/weak_action) | Resolve vazamentos de memória em assinaturas de eventos, implementando tratamento de eventos seguro. |
| [Mecanismo de script ScriptEngine](https://newlifex.com/core/script_engine) | Suporta compilação e execução dinâmica de C#, oferecendo capacidade flexível de extensão por scripts. |
| [Compressão e descompressão](https://newlifex.com/core/compression) | Integra diversos algoritmos de compressão e fornece interface unificada para compressão de dados. |
| [Arquivamento comprimido Tar e 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Cobre usos avançados de arquivamento e compressão com Tar/Tar.Gz e 7Zip. |
| [Informações da máquina MachineInfo](https://newlifex.com/core/machine_info) | Obtém informações de hardware de forma abrangente, com suporte a sistemas Windows e Linux. |
| [Biblioteca de pinyin PinYin](https://newlifex.com/core/pinyin) | Biblioteca eficiente para converter caracteres chineses em pinyin, com suporte a pinyin completo e abreviado e desempenho superior. |
| [Tabela de dados DbTable](https://newlifex.com/core/dbtable) | Tabela de dados em memória, que facilita o processamento e a análise de dados. |
| [Parâmetro de paginação PageParameter](https://newlifex.com/core/page_parameter) | Encapsula a lógica de paginação, com suporte a paginação em diversos cenários, como Web e API. |
| [Algoritmo Snowflake](https://newlifex.com/core/snow_flake) | Algoritmo distribuído de geração de IDs únicos, que garante a unicidade dos IDs sob alta concorrência. |

</details>

<details>
<summary><b>Segurança e tokens</b> (3)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Token de assinatura digital distribuída TokenProvider](https://newlifex.com/core/token_provider) | Gera e valida tokens distribuídos, garantindo segurança entre múltiplos nós. |
| [Token Web universal JwtBuilder](https://newlifex.com/core/jwt) | Oferece geração, decodificação e validação de tokens JWT, simplificando a autenticação Web. |
| [Filtro HTTP de token TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Injeta automaticamente o token nas requisições HTTP e renova automaticamente ao interceptar 401/403. |

</details>

<details>
<summary><b>Biblioteca de rede</b> (13)</summary>

| Funcionalidade | Características principais |
| --- | --- |
| [Servidor de rede NetServer](https://newlifex.com/core/netserver) | Servidor TCP/UDP/Unix domain socket de alto desempenho, que encapsula sessões e processamento de dados. |
| [Cliente de rede NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Cliente de rede TCP/UDP/Unix unificado, que simplifica a conexão e o envio/recebimento. |
| [Mensagem IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Contrato de mensagem de requisição-resposta, que carrega semântica de direção e leitura/escrita de frames. |
| [Pilha de protocolo de mensagens MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Pilha única de frame pump + codec sem estado, com suporte a corpo de frames grandes em streaming e backpressure. |
| [Pipeline de dados Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Pipeline de dados com zero cópia, alinhado ao formato de System.IO.Pipelines. |
| [Protocolo simples de troca de mensagens remotas SRMP](https://newlifex.com/core/srmp) | Protocolo RPC leve, com suporte a serialização binária e transporte UDP. |
| [Balanceamento de carga e failover LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Oferece três estratégias de escalonamento entre múltiplos nós: failover, round-robin e corrida (racing). |
| [Cliente HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Encapsulamento baseado em HttpClient, que simplifica a chamada e o tratamento de Web APIs. |
| [Servidor HTTP HttpServer (dispositivos embarcados)](https://newlifex.com/core/httpserver) | Servidor Http leve, com suporte a pipeline de middleware, rotas parametrizadas e serviço de arquivos estáticos. |
| [Download de rede WebClientX](https://newlifex.com/core/webclient) | Versão aprimorada do WebClient, com suporte a download multithread e retomada de download. |
| [Servidor WebSocket (ferramenta poderosa para notificações downstream em IoT)](https://newlifex.com/core/websocket) | Servidor WebSocket de alto desempenho, adequado para push de mensagens da Internet das Coisas. |
| [Comunicação bidirecional WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Apresenta práticas de handshake, processamento de frames, heartbeat e fechamento de conexão. |
| [Cliente de armazenamento de arquivos EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | Cliente HTTP de armazenamento de arquivos, para EasyIO ou OSS. |

</details>

> RPC (`ApiServer`/`ApiClient`) foi movido para [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting).

## Ecossistema

NewLife é um ecossistema em camadas de projetos open source versionados de forma independente. O mapa de camadas abaixo é compartilhado literalmente por todos os repositórios NewLife; a tabela detalhada acrescenta o posicionamento e a tração de cada projeto.

| Camada | Projetos |
| --- | --- |
| Base | Core · XCode · Net · Remoting · Agent · Templates |
| Dados e mensagens | Redis · RocketMQ · MQTT · NovaDb · MySql |
| Web, IA e distribuído | Cube · Stardust · AntJob · NewLife.AI |
| Industrial e IoT | IoT · Modbus · Siemens · Omron · Melsec · OPC · BACnet · ZeroIoT |
| Telemática veicular | JT808 · JT809 · JT1078 |
| Componentes de domínio | Office · Audio · Video · Map · IP |
| Ferramentas e produtos | CrazyCoder · GitCandy · LuckyClover |

### Detalhes dos projetos

| Projeto | O que faz | Adoção |
| --- | --- | --- |
| **Base** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Log, configuração, cache, rede, serialização, agendamento e rastreamento APM. | ★1.9k · 1.91M |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Middleware de dados: tabelas únicas na escala de dezenas de bilhões de linhas, particionamento automático, separação de leitura e escrita, mais de 10 bancos de dados. | ★104 · 541k |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Biblioteca de rede de alto desempenho: 22,66M tps por máquina, 4M de conexões TCP persistentes. | ★313 · 74.5k |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Framework de comunicação por protocolo e RPC para aplicações cliente/servidor e dispositivos IoT. | ★16 · 272k |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Instala aplicações .NET como daemons do sistema operacional: serviços do Windows e systemd do Linux. | ★45 · 108k |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Modelos de projeto `dotnet new` para console, web, API, serviço, dados e IoT. | ★82 · 14.9k |
| **Dados e mensagens** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Cliente Redis com latência em microssegundos, milhões de operações por segundo e filas de mensagens integradas. | ★878 · 723k |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Cliente RocketMQ totalmente gerenciado para Apache RocketMQ e Alibaba Cloud Message Queue. | ★225 · 120k |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Cliente e servidor MQTT, incluindo conectividade com o Alibaba Cloud IoT. | ★150 · 99.4k |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Banco de dados híbrido em C#: embarcado ou em rede, relacional, séries temporais, fila de mensagens e KV em um só. | ★12 · 3.4k |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Driver MySql e camada auxiliar construídos sobre a pilha de dados NewLife. | ★23 · 8.4k |
| **Web, IA e distribuído** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Framework de plataforma administrativa rápida: permissões, SSO, servidor OAuth e scaffolding CRUD. | ★637 · 186k |
| [Stardust](https://github.com/NewLifeX/Stardust) | Plataforma de serviços distribuídos: gestão de nós, APM, centro de configuração, registro e centro de publicação. | ★354 · 274k |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plataforma distribuída de tarefas e agendamento de dados em tempo real, com particionamento, repetição e reprocessamento. | ★429 · 56.8k |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Cliente unificado para 46 provedores de LLM, além do ChatAI, uma aplicação web de chat pronta para uso. | ★20 · 11.4k |
| **Industrial e IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Biblioteca padrão de IoT que define contratos de protocolo e abstrações de dispositivos. | ★30 · 104k |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Implementação Modbus TCP/RTU/ASCII sobre NewLife.IoT. | ★27 · 48.7k |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocolo Siemens PLC sobre NewLife.IoT. | ★30 · 14.3k |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocolo Omron PLC sobre NewLife.IoT. | ★8 · 11.1k |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocolo Mitsubishi Melsec PLC sobre NewLife.IoT. | ★34 · 10.8k |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Cliente OPC para acesso a dados industriais. | ★9 · — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Pilha do protocolo de automação predial BACnet. | ★8 · 9k |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Plataforma IoT de código aberto leve, com gestão de gateways e dispositivos. | ★10 · — |
| **Telemática veicular** | | |
| NewLife.JT808 | Protocolo de posicionamento veicular JT/T 808: mais de 70 corpos de mensagem, 2011/2013/2019. | — |
| NewLife.JT809 | Protocolo plataforma-a-plataforma JT/T 809 para autoridades de transporte. | — · 135 |
| NewLife.JT1078 | Vídeo veicular JT/T 1078: análise RTP, remux para FLV/fMP4 e transmissão ao vivo. | — |
| **Componentes de domínio** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Um pacote para Excel, Word, PPT, PDF e mais de 15 formatos de documento. | ★11 · 12.9k |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Áudio multiplataforma: codecs, E/S de dispositivos, DSP, contêineres e streaming. | ★5 · 6.6k |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Codecs de vídeo, formatos de contêiner, processamento de frames e streaming. | ★2 · 610 |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interface unificada sobre Baidu, AMap, Tencent e Tianditu, com geocodificação e conversão de coordenadas. | ★15 · 24.8k |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Geolocalização IPv4 com banco de dados offline mapeado em memória e API online como alternativa. | ★16 · 84.7k |
| **Ferramentas e produtos** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Caixa de ferramentas do desenvolvedor: rede, porta serial, criptografia, regex, Modbus e MQTT. | ★330 · 42.1k |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Sistema auto-hospedado de gerenciamento de código-fonte Git. | ★49 · — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Instala automaticamente o runtime .NET correspondente quando iniciado em um host mais antigo. | ★13 · — |

> Os mais de 30 pacotes não listados aqui (FTP, LoRa, extensões do Office, drivers de protocolo e outros) estão disponíveis na [organização NewLifeX](https://github.com/NewLifeX) e em [newlifex.com](https://newlifex.com).

## Documentação

### Guias

- [Catálogo da biblioteca principal](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): índice de artigos dos componentes principais, agrupados por módulo
- [Análise de compatibilidade com AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): escopo do NativeAOT, conclusões medidas (smoke <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), matriz de RID e limites conhecidos
- [Localização](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): modelo de mensagens, tabelas de idiomas, substituições a jusante e adição de idiomas em tempo de execução
- [Checklist de localização para projetos dependentes](https://github.com/NewLifeX/X/blob/master/Doc/下游多语言接入清单.md): etapas de integração, regras de idioma, portões de CI e modelos de badges
- [Arquitetura móvel](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): pilha móvel .NET, escolha de framework de UI, empacotamento APK e notas de AOT
- [Exemplos](https://github.com/NewLifeX/X/tree/master/Samples): exemplos de console, serviço, Http, IoT e móvel

### Notas de versão

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): histórico de versões

## Contribuição

### Compilar e testar

A solução abre no Visual Studio (versão estável mais recente) e também compila com o .NET CLI; os arquivos de projeto permanecem deliberadamente simples para que um clone novo compile imediatamente.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Execução completa em paralelo: `run-tests.ps1` (vários hosts em paralelo, guarda de timeout e resumo trx)
- Smoke test de NativeAOT: `AotSmoke/`, CI em `.github/workflows/aot.yml`
- Benchmarks: `Benchmark/` (BenchmarkDotNet)

### Estrutura do repositório

| Diretório | Descrição |
| --- | --- |
| `NewLife.Core/` | Código-fonte da biblioteca principal (log, rede, cache, serialização, …) |
| `NewLife.Core.SourceGenerator/` | Gerador de código distribuído como `analyzers/` dentro do pacote |
| `Doc/` | Documentação de capacidades e índice de artigos |
| `Samples/` | Exemplos de console / serviço / Http / IoT / móvel |
| `Benchmark/` | Benchmarks de desempenho com BenchmarkDotNet |
| `XUnitTest.Core/` | Testes unitários principais (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Testes de compatibilidade entre versões |
| `AotSmoke/` | Projeto de smoke test NativeAOT |

### Publicação

Envie uma tag com prefixo `v` (por exemplo `v12.0.2026.0927`) para acionar `.github/workflows/publish.yml`. O fluxo empacota o pacote, publica-o no NuGet.org e no GitHub Packages e cria um GitHub Release cujas notas vêm da seção correspondente do `ChangeLog.md` (os ativos incluem nupkg e snupkg). Uma execução manual apenas publica pacotes e não cria Release.

### Documentação de desenvolvimento

O diretório `Doc/` mantém um conjunto de documentação de capacidades que espelha o código: requisitos → rastreamento de funcionalidades → arquitetura. Esses documentos são escritos em chinês.

- [Documento de requisitos](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): visão, objetivos centrais e requisitos funcionais
- [Lista de funcionalidades](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): status de implementação, teste e comentários de cada funcionalidade
- [Design de arquitetura](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): camadas, componentes de módulos e decisões-chave de design

### Feedback

- Antes de enviar, verifique se `dotnet build` e os testes unitários relevantes passam; siga as convenções do repositório para estilo de código e mensagens de commit.
- Relate bugs e sugestões via [Issue](https://github.com/NewLifeX/X/issues); Stars e PRs são bem-vindos.

## Equipe

A NewLife foi fundada em 2002 e atua com .NET, IoT e sistemas distribuídos. Seus componentes de código aberto já foram baixados <!--nuget-total:start-->6.04M<!--nuget-total:end--> vezes no NuGet (até 2026-10) e são usados em projetos de energia, indústria, transporte, meio ambiente, saúde e patrimônio cultural.

Website: https://newlifex.com · Organização de código aberto: https://github.com/NewLifeX  
Conta oficial no WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

## Licença

Este projeto é licenciado sob [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE); o certificado de nome forte `newlife.snk` é publicado para que você possa compilar e substituir os assemblies por conta própria, e o código-fonte pode ser modificado e redistribuído sem atribuição.

> Se este projeto for útil para você, dê uma Star, compartilhe-o com sua equipe ou envie sugestões de melhoria via Issue/PR.


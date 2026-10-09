# NewLife.Core - Biblioteca Principal

![GitHub License](https://img.shields.io/github/license/newlifex/x?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![AtomGit Stars](https://atomgit.com/NewLifeX/X/star/badge.svg)](https://atomgit.com/NewLifeX/X)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=v12)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)

🌐 [中文](Readme.MD) | [English](README.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md) | [العربية](README.ar.md)

O framework NewLife é um ecossistema abrangente de componentes .NET, que oferece infraestrutura de alto desempenho para a construção de aplicações escaláveis. Ele fornece funcionalidades básicas como log, rede, serialização, cache e multithreading, servindo de base para uma série de aplicações, de serviços Web a dispositivos IoT.  

Tutorial da biblioteca principal: [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ Esta tradução pode ficar desatualizada; a versão autorizada é o [README em chinês](Readme.MD).



## Visão geral do framework

O NewLife é composto por várias bibliotecas de componentes, e o NewLife.Core fornece a base. O framework suporta múltiplos frameworks de destino, de net45 a net10.0 (.NET Framework 4.5 até .NET 10.0), proporcionando ampla compatibilidade em diferentes ambientes.

**NativeAOT**: o caminho principal já é suportado (25 cenários testados + CI contra regressões); capacidades dinâmicas como plugins e scripts não são suportadas por design; consulte os limites em [Análise de compatibilidade com AOT](Doc/AOT兼容性分析.md) (中文).



## Índice de documentação

O diretório `Doc/` do repositório mantém o sistema de documentação de capacidades (definição de requisitos → rastreamento de funcionalidades → design de arquitetura):

- [Documento de requisitos](Doc/需求文档.md) (中文): visão, objetivos centrais e requisitos funcionais
- [Lista de funcionalidades](Doc/功能清单.md) (中文): rastreamento do status de implementação/teste/comentários de todos os pontos de funcionalidade
- [Design de arquitetura](Doc/架构设计.md) (中文): camadas, componentes de módulos e decisões-chave de design
- [Catálogo da biblioteca principal](Doc/核心库目录.md) (中文): índice de artigos dos componentes principais
- [Análise de compatibilidade com AOT](Doc/AOT兼容性分析.md) (中文): escopo de suporte do NativeAOT, conclusões de testes reais (smoke 25/25), guia de uso e limites conhecidos

## Início rápido

A maneira mais simples de usar os componentes NewLife é referenciá-los pelo Nuget; por exemplo, pesquise `NewLife.Core` no gerenciador de pacotes Nuget do projeto e adicione-o.

Os componentes NewLife são criados em conjunto pela comunidade há mais de 20 anos e adotam a licença open source MIT; **qualquer pessoa pode modificá-los e redistribuí-los livremente** (sem necessidade de declarar a origem)! Muitas empresas, ao construir frameworks internos de desenvolvimento com base neles, podem até realizar personalização privada substituindo em massa todas as strings `NewLife` no código-fonte pelo nome da sua empresa.

A equipe sempre mantém uma postura aberta: além de suportar a compilação ao abrir a solução no VisualStudio (versão estável mais recente), também é compatível com a compilação pela linha de comando `dotnet build`. Os arquivos de projeto abandonam funcionalidades complexas em busca de simplicidade e facilidade de uso, sendo realmente prontos para usar desde o primeiro momento.

Divulgamos publicamente o certificado de nome forte `newlife.snk` para permitir compilar e substituir os assemblies por conta própria.



Execute o seguinte comando na linha de comando para experimentar rapidamente os componentes NewLife:

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```



## Principais funcionalidades


| Categoria do módulo | Ponto de funcionalidade | Principais características |
| ---------------- | ---------------------------------------------------------------------------- | ------------------------------------------------ |
| **Extensões básicas** | [Conversão de tipos Utility](https://newlifex.com/core/utility) | Fornece conversão de tipos eficiente, com suporte a vários formatos e tratamento de exceções. |
|                  | [Extensão de strings StringHelper](https://newlifex.com/core/string_helper) | Inclui diversas operações de string, como corte, codificação e formatação. |
|                  | [Extensão de caminhos PathHelper](https://newlifex.com/core/path_helper) | Trata de forma inteligente vários formatos de caminho, garantindo compatibilidade do código entre plataformas. |
|                  | [Extensão de dados IOHelper](https://newlifex.com/core/io_helper) | Otimiza operações de IO, oferecendo conversão eficiente entre fluxos de dados e arrays de bytes. |
|                  | [Extensão de processos ProcessHelper](https://newlifex.com/core/process_helper) | Simplifica operações com processos, oferecendo métodos práticos para obter e gerenciar informações de processos. |
|                  | [Informações de runtime Runtime](https://newlifex.com/core/runtime) | Oferece métodos simples para obter informações de runtime da aplicação, facilitando depuração e diagnóstico. |
|                  | [Descartável DisposeBase](https://newlifex.com/core/disposebase) | Fornece um padrão padronizado de liberação de recursos, prevenindo efetivamente vazamentos de memória e de recursos. |
|                  | [Extensão de reflexão Reflect](https://newlifex.com/core/reflect) | Ferramenta de reflexão de alto desempenho, para leitura e escrita rápidas de membros de objetos. |
|                  | [Extensão de segurança SecurityHelper](https://newlifex.com/core/security_helper) | Integra RSA/AES/DES/MD5 e diversos outros algoritmos de criptografia e descriptografia, simplificando operações de segurança. |
| **Log e rastreamento** | [Log ILog](https://newlifex.com/core/log) | Suporta várias formas de saída de log e oferece poderosa gravação assíncrona. |
|                  | [Rastreamento distribuído ITracer](https://newlifex.com/core/tracer) | Poderoso rastreamento de desempenho APM, com suporte à integração com a plataforma Stardust. |
| **Agendamento** | [Temporizador avançado TimerX](https://newlifex.com/core/timerx) | Temporizador de alta precisão, com suporte a agendamento Cron e execução periódica assíncrona. |
|                  | [Expressão Cron](https://newlifex.com/core/cron) | Oferece tarefas agendadas semelhantes ao Cron do Linux, com correspondência no nível de segundos. |
|                  | [Provedor de tempo TimeProvider](/core/time_provider) | Abstração de tempo unificada e cálculo de duração de alta precisão, facilitando a compatibilidade entre versões. |
| **Sistema de configuração** | [Sistema de configuração Config](https://newlifex.com/core/config) | Framework de configuração unificado, com suporte a múltiplos formatos, múltiplas fontes e mecanismo de recarga a quente. |
|                  | [Provedor de configuração IConfigProvider](/core/config_provider) | Interface de configuração unificada, com suporte a callbacks e ao modelo de nós de configuração. |
|                  | [Configuração central Setting](https://newlifex.com/core/setting) | Gerencia as configurações centrais dos componentes, controlando comportamentos básicos como log e rede. |
|                  | [Configuração de sistema SysConfig](https://newlifex.com/core/sysconfig) | Gerencia as configurações do sistema da aplicação, com suporte a configurações locais, distribuídas e criptografadas. |
| **Serialização** | [Serialização Json](https://newlifex.com/core/json) | Ferramenta Json de alto desempenho, com suporte a serialização em diversos cenários complexos. |
|                  | [Serialização Xml](https://newlifex.com/core/xml) | Oferece serialização e desserialização XML, com conversão bidirecional entre objetos e XML. |
|                  | [Serialização binária](https://newlifex.com/core/binary) | Serialização binária de alto desempenho, com suporte a grafos de objetos complexos e controle de versão. |
|                  | [Serialização binária avançada](/core/binary_advanced) | Guia avançado de codificação/decodificação com zero alocações em todo o fluxo de mensagens de protocolo. |
|                  | [Análise de Csv CsvFile](https://newlifex.com/core/csv_file) | Leitura e escrita rápidas de arquivos CSV, com suporte a delimitadores personalizados e vinculação de dados. |
|                  | [Banco de dados Csv CsvDb](https://newlifex.com/core/csv_db) | Usa arquivos CSV como banco de dados, com suporte a operações básicas de CRUD. |
|                  | [Leitor leve de Excel ExcelReader](https://newlifex.com/core/excel_reader) | Sem dependência do Office, lê Excel rapidamente, com suporte a grandes volumes de dados. |
| **Cache** | [Interface de cache unificada ICache](https://newlifex.com/core/icache) | Define operações padrão de cache e oferece diversas implementações, como memória e Redis. |
|                  | [Arquitetura de cache ICacheProvider](https://newlifex.com/core/icacheprovider) | Usada para gerenciar e fornecer instâncias de ICache, com suporte à coexistência de múltiplos caches. |
|                  | [Cache em memória MemoryCache](https://newlifex.com/core/memory_cache) | Cache em memória de máquina única e alto desempenho, com suporte a políticas de expiração e capacidade. |
|                  | [Pool de objetos ObjectPool](https://newlifex.com/core/object_pool) | Reutiliza objetos custosos, melhorando o desempenho e reduzindo a pressão sobre o GC. |
|                  | [Pool de objetos Pool](/core/pool) | Pool de objetos genérico de alto desempenho baseado em CAS, que reduz a pressão sobre o GC. |
|                  | [Cache distribuído NewLife.Redis](https://newlifex.com/core/redis) | Implementação de cache distribuído baseada em Redis, com suporte a diversas estruturas de dados. |
|                  | [Armazenamento vetorial IVectorStore](/core/vector_store) | Interface de gerenciamento de coleções de vetores, com suporte a busca de similaridade Top-K, fornecendo infraestrutura de busca vetorial para aplicações de IA. |
| **Buffers de alto desempenho** | [Pacote de dados IPacket](https://newlifex.com/core/packet) | Interface central de pacote de dados, com fatias zero-copy e pacotes em cadeia. |
|                  | [Buffers](/core/buffers) | Biblioteca de operações de buffer de alto desempenho, que reúne componentes como Span e escrita em pool. |
|                  | [Escritor em pool PooledByteBufferWriter](/core/pooled_buffer_writer) | Escritor de buffer em pool baseado em ArrayPool, com suporte a expansão dinâmica. |
|                  | [Leitor Span SpanReader](/core/span_reader) | Analisador binário de alto desempenho com zero alocação, com suporte a big-endian e little-endian. |
|                  | [Escritor Span SpanWriter](/core/span_writer) | Escritor de dados binários de alto desempenho com zero alocação, com suporte a big-endian e little-endian. |
|                  | [Auxiliar de Span SpanHelper](/core/span_helper) | Conjunto de métodos de extensão para Span/ReadOnlySpan, simplificando operações de memória. |
|                  | [Leitor sequencial SequenceReader](/core/sequence_reader) | Leitor sequencial entre segmentos (em frameworks antigos, suprido por shim; disponível em todos os 18 TFMs). |
| **Framework de aplicação** | [Contêiner de objetos ObjectContainer](https://newlifex.com/core/object_container) | Injeção de dependência leve, com suporte a vários ciclos de vida e modos de injeção. |
|                  | [Host de aplicação leve Host](https://newlifex.com/core/host) | Simplifica a hospedagem de aplicações, facilitando a construção de serviços em segundo plano de longa duração. |
|                  | [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Oferece mecanismo padronizado de gerenciamento de plugins, viabilizando o desenvolvimento modular de aplicações. |
|                  | [Modelo de concorrência Actor](https://newlifex.com/core/actor) | Modelo simplificado de programação concorrente, que melhora a vazão por meio do processamento em fila de mensagens. |
|                  | [Modelo de pipeline Pipeline](/core/pipeline) ⚠️ Descontinuado, substituído pelo modelo de protocolo (frame pump) | Modelo de cadeia de processamento bidirecional, que unifica os fluxos Read/Write/Open/Close. |
|                  | [Fila de atraso DeferredQueue](/core/deferred_queue) | Agrega alterações de alta frequência e as persiste em lote, reduzindo a amplificação de escrita. |
|                  | [Barramento de eventos EventBus](/core/eventbus) | Distribuição de eventos em processo, com suporte a roteamento por tópico e mensagens em fila. |
|                  | [Delegado de referência fraca WeakAction](https://newlifex.com/core/weak_action) | Resolve vazamentos de memória em assinaturas de eventos, implementando tratamento de eventos seguro. |
|                  | [Mecanismo de script ScriptEngine](https://newlifex.com/core/script_engine) | Suporta compilação e execução dinâmica de C#, oferecendo capacidade flexível de extensão por scripts. |
|                  | [Compressão e descompressão](https://newlifex.com/core/compression) | Integra diversos algoritmos de compressão e fornece interface unificada para compressão de dados. |
|                  | [Arquivamento comprimido Tar e 7Zip](/core/tar_7zip) | Cobre usos avançados de arquivamento e compressão com Tar/Tar.Gz e 7Zip. |
|                  | [Informações da máquina MachineInfo](https://newlifex.com/core/machine_info) | Obtém informações de hardware de forma abrangente, com suporte a sistemas Windows e Linux. |
|                  | [Biblioteca de pinyin PinYin](https://newlifex.com/core/pinyin) | Biblioteca eficiente para converter caracteres chineses em pinyin, com suporte a pinyin completo e abreviado e desempenho superior. |
|                  | [Conjunto de dados DbTable](https://newlifex.com/core/dbtable) | Tabela de dados em memória, que facilita o processamento e a análise de dados. |
|                  | [Parâmetro de paginação PageParameter](https://newlifex.com/core/page_parameter) | Encapsula a lógica de paginação, com suporte a paginação em diversos cenários, como Web e API. |
|                  | [Algoritmo Snowflake](https://newlifex.com/core/snow_flake) | Algoritmo distribuído de geração de IDs únicos, que garante a unicidade dos IDs sob alta concorrência. |
| **Segurança e tokens** | [Token de assinatura digital distribuída TokenProvider](https://newlifex.com/core/token_provider) | Usado para gerar e validar tokens distribuídos, garantindo segurança entre múltiplos nós. |
|                  | [Token Web universal JwtBuilder](https://newlifex.com/core/jwt) | Oferece geração, decodificação e validação de tokens JWT, simplificando a autenticação Web. |
|                  | [Filtro HTTP de token TokenHttpFilter](https://newlifex.com/core/token_http_filter) | Injeta automaticamente o token nas requisições HTTP e renova automaticamente ao interceptar 401/403. |
| **Serviços em nuvem** | [Hash de latitude e longitude GeoHash](https://newlifex.com/core/geo_hash) | Codifica latitude e longitude como strings, para uso em busca de locais próximos. |
|                  | ⚠️ Mapas Baidu/Amap foram extraídos para pacotes NuGet independentes. |                                                  |
| **Biblioteca de rede** | [Servidor de rede NetServer](https://newlifex.com/core/netserver) | Servidor TCP/UDP/Unix domain socket de alto desempenho, que encapsula sessões e processamento de dados. |
|                  | [Cliente de rede NetClient](/core/netclient) | Cliente de rede TCP/UDP/Unix unificado, que simplifica a conexão e o envio/recebimento. |
|                  | [Mensagem IMessage](/core/message) | Contrato de mensagem de requisição-resposta, que carrega semântica de direção e leitura/escrita de frames. |
|                  | [Pilha de protocolo de mensagens MessagePump](/core/message_protocol) | Pilha única de frame pump + codec sem estado, com suporte a corpo de frames grandes em streaming e backpressure. |
|                  | [Pipeline de dados Pipe](/core/packet_pipe) | Pipeline de dados com zero cópia, alinhado ao formato de System.IO.Pipelines. |
|                  | [Protocolo simples de troca de mensagens remotas SRMP](https://newlifex.com/core/srmp) | Protocolo RPC leve, com suporte a serialização binária e transporte UDP. |
|                  | [Balanceamento de carga e failover LoadBalancer](/core/load_balancer) | Oferece três estratégias de escalonamento entre múltiplos nós: failover, round-robin e corrida (racing). |
|                  | [Comunicação RPC ApiServer](https://newlifex.com/core/api_server) | ⚠️ Movido para [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
|                  | [Comunicação RPC ApiClient](https://newlifex.com/core/api_client) | ⚠️ Movido para [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
|                  | [Cliente HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Encapsulamento baseado em HttpClient, que simplifica a chamada e o tratamento de Web APIs. |
|                  | [Servidor HTTP HttpServer (dispositivos embarcados)](https://newlifex.com/core/httpserver) | Servidor Http leve, com suporte a pipeline de middleware, rotas parametrizadas e serviço de arquivos estáticos. |
|                  | [Download de rede WebClientX](https://newlifex.com/core/webclient) | Versão aprimorada do WebClient, com suporte a download multithread e retomada de download. |
|                  | [Servidor WebSocket (ferramenta definitiva de notificação downstream para IoT)](https://newlifex.com/core/websocket) | Servidor WebSocket de alto desempenho, adequado para push de mensagens da Internet das Coisas. |
|                  | [Comunicação bidirecional WebSocket](/core/websocket_guide) | Apresenta práticas de handshake, processamento de frames, heartbeat e fechamento de conexão. |
|                  | [Cliente de armazenamento de arquivos EasyClient](/core/easy_io) | Cliente HTTP de armazenamento de arquivos, integrado ao EasyIO ou OSS. |



## Matriz de projetos NewLife
Todos os projetos suportam por padrão net10.0/netstandard2.1/netstandard2.0/net4.62/net4.5; as versões antigas (2024.0801) suportam net4.0/net2.0  

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
# Equipe de desenvolvimento NewLife
![XCode](https://newlifex.com/logo.png)

A equipe NewLife foi fundada em 2002 e é fornecedora de soluções para o setor de Internet das Coisas da nova era, dedicada a fornecer consultoria de soluções de software e hardware, planejamento de arquitetura de sistemas e serviços de desenvolvimento. O NuGet acumula mais de 5,23 milhões de downloads. Os componentes principais da equipe (XCode / Redis / Stardust / AntJob / IoT, etc.) já são amplamente aplicados em setores como energia elétrica, indústria, transporte, proteção ambiental, saúde, museus e patrimônio cultural.

Melhoramos continuamente e buscamos ser um parceiro de tecnologia .NET / IoT / distribuída digno de confiança de longo prazo.

Site: https://newlifex.com   Organização open source: https://github.com/NewLifeX  
Conta oficial no WeChat:  
![智能大石头](https://newlifex.com/stone.jpg)

---
> Se este projeto for útil para você, seja bem-vindo a dar uma Star, compartilhar com sua equipe ou enviar sugestões de melhoria por Issue/PR.

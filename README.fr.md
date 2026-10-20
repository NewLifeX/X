# NewLife.Core - Bibliothèque principale

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** est le socle de l'écosystème NewLife : un seul paquet pour la journalisation, la configuration, le cache, le réseau, la sérialisation, l'ordonnancement, le conteneur d'objets et le traçage APM. Il est prêt pour NativeAOT, fournit les messages d'exécution en <!--locale-count:start-->31<!--locale-count:end--> langues, cible 11 frameworks de net45 à net10.0 et alimente XCode, Cube, Redis, Stardust et plus de 30 projets frères.

Tutoriel de la bibliothèque principale : [https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ Cette traduction peut être en retard ; la version de référence est le [README anglais](README.md).

**Sommaire**: [Démarrage rapide](#démarrage-rapide) · [Pourquoi NewLife.Core](#pourquoi-newlifecore) · [Fonctionnalités principales](#fonctionnalités-principales) · [Carte de l'écosystème](#carte-de-lécosystème) · [Documentation](#documentation) · [Contribution](#contribution) · [Équipe](#équipe) · [Licence](#licence)

## Démarrage rapide

### Installation

```bash
dotnet add package NewLife.Core
```

### Exemple minimal

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### Modèle de projet

Exécutez les commandes suivantes dans une ligne de commande pour découvrir rapidement les composants NewLife :

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### Prise en charge des plateformes

| Élément | Couverture |
| --- | --- |
| Frameworks cibles | 11 dans un seul paquet : net45, net461, net462, netstandard2.0, netstandard2.1, net6.0, net6.0-windows, net8.0, net8.0-windows, net10.0, net10.0-windows |
| Système d'exploitation et architecture | Windows, Linux, macOS, Android, iOS ; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> scénarios du cœur mesurés ; matrice de smoke CI multi-RID (win-x64, linux-x64, linux-arm64 et linux-loongarch64 sont des garde-fous bloquants). Les plugins et le scripting ne sont pas compatibles AOT par conception — voir l'[analyse de compatibilité AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |
| Localisation à l'exécution | <!--locale-count:start-->31<!--locale-count:end--> langues intégrées ; les messages de log et d'exception sont localisés à l'exécution sans modifier les points d'appel (voir [Localisation](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)) |
| Frameworks hérités | net40 : `dotnet add package NewLife.Core --version 10.10.2024.801-net40` (net40/net45/net461) ; net20 : `--version 10.10.2024.701-net20` (net20 uniquement). Builds figés de 2024-08 qui ne suivent pas les mises à jour de la ligne principale. |

## Pourquoi NewLife.Core

- **Un seul paquet pour l'infrastructure courante** : journalisation, configuration, cache, réseau, sérialisation, ordonnancement, conteneur d'objets et DI — une seule référence suffit.
- **Large couverture, exécution moderne** : 11 frameworks cibles dans un seul paquet (net45 → net10.0, variantes Windows incluses), prêt pour NativeAOT sur le chemin principal et messages d'exécution localisés en <!--locale-count:start-->31<!--locale-count:end--> langues — la même API pour les systèmes existants et les nouveaux projets.
- **Éprouvé à l'échelle de l'écosystème** : la même base fait tourner XCode (tables uniques de 10 milliards de lignes), Net (1,77 M pps / 14,5 Gbps, TCP 1KB), Redis et Stardust (des dizaines de milliards d'enregistrements).
- **Dépendances maîtrisées, licence permissive** : uniquement la BCL et les paquets de compatibilité Microsoft ; licence MIT, vous pouvez donc modifier et renommer (voir [Licence](#licence)).

## Fonctionnalités principales

<details>
<summary><b>Extensions de base</b> (10)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Conversion de types Utility](https://newlifex.com/core/utility) | Fournit une conversion de types efficace, avec prise en charge de divers formats et gestion des exceptions. |
| [Extensions de chaînes StringHelper](https://newlifex.com/core/string_helper) | Inclut de nombreuses opérations sur les chaînes, comme la troncature, l'encodage et le formatage. |
| [Extensions de chemins PathHelper](https://newlifex.com/core/path_helper) | Gère intelligemment plusieurs formats de chemins pour assurer la compatibilité multiplateforme du code. |
| [Extensions de données IOHelper](https://newlifex.com/core/io_helper) | Optimise les opérations d'E/S et offre une conversion efficace entre flux de données et tableaux d'octets. |
| [Extensions de processus ProcessHelper](https://newlifex.com/core/process_helper) | Simplifie les opérations sur les processus avec des méthodes pratiques pour obtenir et gérer les informations de processus. |
| [Informations d'exécution Runtime](https://newlifex.com/core/runtime) | Fournit des méthodes simples pour obtenir les informations d'exécution de l'application, facilitant le débogage et le diagnostic. |
| [Objet jetable DisposeBase](https://newlifex.com/core/disposebase) | Fournit un modèle standard de libération des ressources qui prévient efficacement les fuites de mémoire et de ressources. |
| [Extensions de réflexion Reflect](https://newlifex.com/core/reflect) | Outil de réflexion hautes performances pour lire et écrire rapidement les membres d'objets. |
| [Extensions de sécurité SecurityHelper](https://newlifex.com/core/security_helper) | Intègre plusieurs algorithmes de chiffrement et déchiffrement comme RSA/AES/DES/MD5, simplifiant les opérations de sécurité. |
| [Hachage latitude/longitude GeoHash](https://newlifex.com/core/geo_hash) | Encode la latitude et la longitude en chaînes pour la recherche de lieux à proximité. |

</details>

<details>
<summary><b>Journalisation et traçage</b> (2)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Journalisation ILog](https://newlifex.com/core/log) | Prend en charge plusieurs modes de sortie de journaux et offre une écriture asynchrone puissante. |
| [Traçage distribué ITracer](https://newlifex.com/core/tracer) | Traçage de performance APM puissant, avec prise en charge de l'intégration à la plateforme Stardust. |

</details>

<details>
<summary><b>Planification et minutage</b> (3)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Minuteur avancé TimerX](https://newlifex.com/core/timerx) | Minuteur haute précision prenant en charge la planification Cron et l'exécution périodique asynchrone. |
| [Expressions Cron](https://newlifex.com/core/cron) | Fournit des tâches planifiées de type Cron Linux, avec une correspondance à la seconde. |
| [Fournisseur de temps TimeProvider](https://newlifex.com/core/timerx_timeprovider) | Abstraction de temps unifiée et calcul de durée haute précision, pour faciliter la compatibilité entre versions. |

</details>

<details>
<summary><b>Système de configuration</b> (4)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Système de configuration Config](https://newlifex.com/core/config) | Framework de configuration unifié prenant en charge plusieurs formats, plusieurs sources et le rechargement à chaud. |
| [Fournisseur de configuration IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | Interface de configuration unifiée prenant en charge les callbacks et un modèle de nœuds de configuration. |
| [Configuration du noyau Setting](https://newlifex.com/core/setting) | Gère la configuration des composants du cœur et contrôle les comportements de base comme la journalisation et le réseau. |
| [Configuration système SysConfig](https://newlifex.com/core/sysconfig) | Gère la configuration système de l'application, avec prise en charge d'une configuration locale, distribuée et chiffrée. |

</details>

<details>
<summary><b>Sérialisation</b> (7)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Sérialisation Json](https://newlifex.com/core/json) | Outil JSON hautes performances prenant en charge la sérialisation dans divers scénarios complexes. |
| [Sérialisation Xml](https://newlifex.com/core/xml) | Fournit la sérialisation et la désérialisation XML, avec conversion bidirectionnelle entre objets et XML. |
| [Sérialisation binaire](https://newlifex.com/core/binary) | Sérialisation binaire hautes performances prenant en charge les graphes d'objets complexes et le contrôle de version. |
| [Sérialisation binaire avancée](https://newlifex.com/core/advanced_binary) | Guide avancé de l'encodage et du décodage sans allocation sur l'ensemble du flux de messages de protocole. |
| [Analyse Csv CsvFile](https://newlifex.com/core/csv_file) | Lecture et écriture rapides de fichiers CSV, avec prise en charge de délimiteurs personnalisés et de la liaison de données. |
| [Base de données Csv CsvDb](https://newlifex.com/core/csv_db) | Utilise des fichiers CSV comme base de données, avec prise en charge des opérations de création, lecture, mise à jour et suppression. |
| [Lecteur Excel léger ExcelReader](https://newlifex.com/core/excel_reader) | Lit Excel rapidement sans dépendance à Office, avec prise en charge de gros volumes de données. |

</details>

<details>
<summary><b>Cache</b> (7)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Interface de cache unifiée ICache](https://newlifex.com/core/icache) | Définit des opérations de cache standard et fournit plusieurs implémentations comme la mémoire et Redis. |
| [Architecture de cache ICacheProvider](https://newlifex.com/core/icacheprovider) | Gère et fournit les instances ICache, avec prise en charge de plusieurs caches coexistants. |
| [Cache mémoire MemoryCache](https://newlifex.com/core/memory_cache) | Cache mémoire local hautes performances prenant en charge les politiques d'expiration et de capacité. |
| [Pool d'objets ObjectPool](https://newlifex.com/core/object_pool) | Réutilise des objets coûteux pour améliorer les performances et réduire la pression sur le GC. |
| [Pool d'objets Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | Pool d'objets générique hautes performances basé sur CAS, qui réduit la pression sur le GC. |
| [Cache distribué NewLife.Redis](https://newlifex.com/core/redis) | Implémentation de cache distribué basée sur Redis, avec prise en charge de multiples structures de données. |
| [Stockage vectoriel IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | Interface de gestion de collections de vecteurs prenant en charge la recherche de similarité Top-K, offrant l'infrastructure de recherche vectorielle pour les applications d'IA. |

</details>

<details>
<summary><b>Tampons hautes performances</b> (7)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Paquet de données IPacket](https://newlifex.com/core/packet) | Interface centrale de paquets de données pour le découpage sans copie et les paquets chaînés. |
| [Tampons Buffers](https://newlifex.com/core/buffers) | Bibliothèque d'opérations de tampon hautes performances regroupant des composants comme Span et les writers mis en pool. |
| [Écrivain mis en pool PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | Écrivain de tampon mis en pool basé sur ArrayPool, avec prise en charge de l'extension dynamique. |
| [Lecteur Span SpanReader](https://newlifex.com/core/span_reader) | Analyseur de flux binaires hautes performances sans allocation, avec prise en charge du little/big endian. |
| [Écrivain Span SpanWriter](https://newlifex.com/core/span_writer) | Écrivain de données binaires hautes performances sans allocation, avec prise en charge du little/big endian. |
| [Assistant Span SpanHelper](https://newlifex.com/core/span_helper) | Ensemble de méthodes d'extension Span/ReadOnlySpan qui simplifie les opérations mémoire. |
| [Lecteur séquentiel SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | Lecteur séquentiel inter-segments (complété par des shims pour les frameworks anciens ; disponible sur les 11 TFM). |

</details>

<details>
<summary><b>Framework applicatif</b> (15)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Conteneur d'objets ObjectContainer](https://newlifex.com/core/object_container) | Injection de dépendances légère prenant en charge plusieurs durées de vie et modes d'injection. |
| [Hôte d'application léger Host](https://newlifex.com/core/host) | Simplifie l'hébergement d'applications pour construire facilement des services d'arrière-plan de longue durée. |
| [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Fournit un mécanisme standardisé de gestion des plugins pour le développement applicatif modulaire. |
| [Modèle de parallélisme Actor](https://newlifex.com/core/actor) | Modèle de programmation concurrente simplifié qui améliore le débit grâce au traitement par file de messages. |
| [File différée DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | Regroupe les modifications à haute fréquence et les persiste par lots, réduisant l'amplification d'écriture. |
| [Bus d'événements EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | Distribution d'événements intra-processus prenant en charge le routage par sujet et les messages de type file. |
| [Délégué à référence faible WeakAction](https://newlifex.com/core/weak_action) | Résout le problème de fuite mémoire des abonnements aux événements et permet une gestion sûre des événements. |
| [Moteur de scripts ScriptEngine](https://newlifex.com/core/script_engine) | Prend en charge la compilation et l'exécution dynamiques de C#, offrant une extensibilité flexible par scripts. |
| [Compression et décompression](https://newlifex.com/core/compression) | Intègre plusieurs algorithmes de compression et fournit une interface unifiée pour compresser les données. |
| [Archives compressées Tar et 7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | Couvre l'utilisation avancée de la compression d'archives pour Tar/Tar.Gz et 7Zip. |
| [Informations machine MachineInfo](https://newlifex.com/core/machine_info) | Obtient des informations matérielles complètes, avec prise en charge des systèmes Windows et Linux. |
| [Bibliothèque PinYin](https://newlifex.com/core/pinyin) | Bibliothèque efficace de conversion des caractères chinois en pinyin, prenant en charge l'écriture complète et abrégée avec des performances supérieures. |
| [Jeu de données DbTable](https://newlifex.com/core/dbtable) | Table de données en mémoire pour un traitement et une analyse des données pratiques. |
| [Paramètres de pagination PageParameter](https://newlifex.com/core/page_parameter) | Encapsule la logique de pagination, avec prise en charge dans des scénarios tels que le Web et l'API. |
| [Algorithme Snowflake](https://newlifex.com/core/snow_flake) | Algorithme de génération d'identifiants uniques distribués garantissant l'unicité sous forte concurrence. |

</details>

<details>
<summary><b>Sécurité et jetons</b> (3)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Jeton à signature numérique distribuée TokenProvider](https://newlifex.com/core/token_provider) | Génère et valide des jetons distribués, garantissant la sécurité entre plusieurs nœuds. |
| [Jeton Web générique JwtBuilder](https://newlifex.com/core/jwt) | Fournit la génération, le décodage et la validation des jetons JWT, simplifiant l'authentification Web. |
| [Filtre HTTP de jetons TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | Injecte automatiquement le jeton dans les requêtes HTTP et intercepte les 401/403 pour le renouveler automatiquement. |

</details>

<details>
<summary><b>Bibliothèque réseau</b> (13)</summary>

| Fonctionnalité | Caractéristiques principales |
| --- | --- |
| [Serveur réseau NetServer](https://newlifex.com/core/netserver) | Serveur de sockets TCP/UDP/Unix hautes performances encapsulant les sessions et la gestion des données. |
| [Client réseau NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | Client réseau TCP/UDP/Unix unifié qui simplifie la connexion et l'envoi/réception. |
| [Message IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | Contrat de message requête-réponse portant la sémantique de direction et la lecture/écriture de trames. |
| [Pile de protocole de messages MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | Pile unique pompe de trames + codec sans état prenant en charge les corps de trame en streaming volumineux et la contre-pression. |
| [Pipeline de données Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | Pipeline de données sans copie aligné sur la forme de System.IO.Pipelines. |
| [Protocole simple d'échange de messages à distance SRMP](https://newlifex.com/core/srmp) | Protocole RPC léger prenant en charge la sérialisation binaire et le transport UDP. |
| [Équilibrage de charge et basculement LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | Propose trois stratégies d'ordonnancement multi-nœuds : basculement, interrogation et course. |
| [Client HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Basé sur une enveloppe HttpClient, il simplifie l'appel et la gestion des API Web. |
| [Serveur HTTP HttpServer (appareils embarqués)](https://newlifex.com/core/httpserver) | Serveur HTTP léger prenant en charge les pipelines de middleware, le routage paramétré et la diffusion de fichiers statiques. |
| [Téléchargement réseau WebClientX](https://newlifex.com/core/webclient) | WebClient amélioré prenant en charge les téléchargements multithread et reprenables. |
| [Serveur WebSocket (outil idéal pour les notifications descendantes IoT)](https://newlifex.com/core/websocket) | Serveur WebSocket hautes performances adapté à la notification de messages IoT. |
| [Communication bidirectionnelle WebSocket](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | Présente les pratiques de handshake, de gestion des trames, de heartbeat et de fermeture de connexion. |
| [Client de stockage de fichiers EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | Client HTTP de stockage de fichiers pour EasyIO ou OSS. |

</details>

> RPC (`ApiServer`/`ApiClient`) a été déplacé vers [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting).

## Carte de l'écosystème

NewLife est un écosystème en couches de projets open source versionnés indépendamment. La carte ci-dessous est reprise à l'identique par tous les dépôts NewLife ; chaque projet renvoie à son dépôt et à son paquet NuGet principal.

| Projet | Rôle | Téléchargements |
| --- | --- | --- |
| **Fondation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Journalisation, configuration, cache, réseau, sérialisation, ordonnancement et traçage APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Intergiciel de données : tables uniques à l'échelle de dizaines de milliards, partitionnement automatique, séparation lecture/écriture et plus de 10 bases de données. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Bibliothèque réseau hautes performances : 1,77 M pps / 14,5 Gbps sur une machine (TCP 1KB, 2026-10), 4 M connexions TCP persistantes. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Communication protocolaire et framework RPC pour applications client/serveur et appareils IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installe des applications .NET comme démons système : services Windows et systemd Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Modèles de projet `dotnet new` pour console, web, API, service, données et IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Données et messagerie** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Client Redis avec une latence en microsecondes, des millions d'opérations/s et des files de messages intégrées. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Client RocketMQ entièrement managé pour Apache RocketMQ et Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Client et serveur MQTT, y compris la connectivité Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Base de données hybride en C# : embarquée ou en réseau, relationnelle, séries temporelles, file de messages et KV réunis. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Pilote et couche d'assistance MySql construits sur la pile de données NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, IA et distribué** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Framework rapide de plateforme d'administration : permissions, SSO, serveur OAuth et échafaudage CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Plateforme de services distribués : gestion des nœuds, APM, centre de configuration, registre et centre de publication. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plateforme de tâches distribuées et d'ordonnancement de données en temps réel avec partitionnement, reprises et backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Client unifié pour 46 fournisseurs de LLM, plus ChatAI, une application web de chat prête à l'emploi. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industriel et IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Bibliothèque standard IoT définissant les contrats de protocole et les abstractions d'appareils. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Implémentation Modbus TCP/RTU/ASCII au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocole pour automate Siemens au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocole pour automate Omron au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocole pour automate Mitsubishi Melsec au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Client OPC pour l'accès aux données industrielles. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Pile de protocoles BACnet pour l'automatisation des bâtiments. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Plateforme IoT open source légère avec gestion des passerelles et des appareils. | — |
| **Télématique embarquée** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | Protocole de positionnement de véhicules JT/T 808 : plus de 70 corps de message, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | Protocole plateforme à plateforme JT/T 809 pour les autorités de transport. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | Vidéo embarquée JT/T 1078 : analyse RTP, remuxage FLV/fMP4 et diffusion en direct. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Composants métier** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Un seul paquet pour Excel, Word, PPT, PDF et plus de 15 formats de documents. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Audio multiplateforme : codecs, E/S d'appareils, DSP, conteneurs et streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Codecs vidéo, formats de conteneurs, traitement des images et streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interface unifiée pour Baidu, AMap, Tencent et Tianditu avec géocodage et conversion de coordonnées. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Géolocalisation IPv4 avec une base de données hors ligne mappée en mémoire et une API en ligne en secours. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Outils et produits** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Boîte à outils du développeur : réseau, port série, cryptographie, regex, Modbus et MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Système de gestion de code source Git auto-hébergé. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installe automatiquement le runtime .NET correspondant lorsqu'il est lancé depuis un hôte plus ancien. | — |

> Les badges indiquent le paquet principal de chaque projet ; les paquets de thèmes, d'extensions et satellites sont répertoriés sur leurs pages NuGet. Les plus de 30 paquets non listés ici (FTP, LoRa, extensions Office, pilotes de protocoles, etc.) sont disponibles dans l'[organisation NewLifeX](https://github.com/NewLifeX) et sur [newlifex.com](https://newlifex.com).

## Documentation

### Guides

- [Sommaire de la bibliothèque principale](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md): index des articles sur les composants du cœur, regroupés par module
- [Analyse de compatibilité AOT](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md): périmètre NativeAOT, conclusions mesurées (smoke <!--aot-smoke:start-->31/31<!--aot-smoke:end-->), matrice de RID et limites connues
- [Localisation](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md): langues prises en charge et utilisation de base
- [Architecture mobile](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md): pile mobile .NET, choix du framework UI, empaquetage APK et notes AOT
- [Exemples](https://github.com/NewLifeX/X/tree/master/Samples): exemples console, service, Http, IoT et mobile

### Notes de version

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md): historique des versions

## Contribution

### Compiler et tester

La solution s'ouvre dans Visual Studio (dernière version) et se compile également avec la CLI .NET ; les fichiers de projet restent volontairement simples afin qu'un clone frais se compile sans configuration supplémentaire.

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- Exécution entièrement parallèle : `run-tests.ps1` (parallélisme multi-hôtes, garde-fou de délai d'expiration et résumé trx)
- Smoke NativeAOT : `AotSmoke/`, CI dans `.github/workflows/aot.yml`
- Benchmarks : `Benchmark/` (BenchmarkDotNet)

### Structure du dépôt

| Répertoire | Description |
| --- | --- |
| `NewLife.Core/` | Sources de la bibliothèque principale (journalisation, réseau, cache, sérialisation, …) |
| `NewLife.Core.SourceGenerator/` | Générateur de sources livré sous `analyzers/` dans le paquet |
| `Doc/` | Documentation des capacités et index des articles |
| `Samples/` | Exemples console / service / Http / IoT / mobile |
| `Benchmark/` | Benchmarks de performance BenchmarkDotNet |
| `XUnitTest.Core/` | Tests unitaires principaux (net10.0) |
| `XUnitTest.Compat/`, `XUnitTest.Net6/` | Tests de compatibilité entre versions |
| `AotSmoke/` | Projet de smoke NativeAOT |

### Publication

Poussez une étiquette préfixée par `v` (par exemple `v12.0.2026.0927`) pour déclencher `.github/workflows/publish.yml`. Le workflow empaquette le paquet, le publie sur NuGet.org et GitHub Packages, et crée une GitHub Release dont les notes proviennent de la section correspondante de `ChangeLog.md` (les artefacts incluent nupkg et snupkg). Une exécution manuelle publie uniquement les paquets et ne crée pas de Release.

### Documentation de développement

Le répertoire `Doc/` maintient un ensemble de documentation des capacités qui reflète le code : exigences → suivi des fonctionnalités → architecture. Ces documents sont rédigés en chinois.

- [Document d'exigences](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md): vision, objectifs principaux et exigences fonctionnelles
- [Liste des fonctionnalités](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md): état d'implémentation, de test et de commentaires de chaque fonctionnalité
- [Conception de l'architecture](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md): couches, composants et décisions de conception clés

### Retours

- Avant de soumettre, assurez-vous que `dotnet build` et les tests unitaires concernés passent ; suivez les conventions du dépôt pour le style de code et les messages de commit.
- Signalez les bogues et suggestions via [Issue](https://github.com/NewLifeX/X/issues) ; les Stars et les PR sont les bienvenues.

## Équipe

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

NewLife a été fondée en 2002 et œuvre depuis plus de deux décennies dans l'écosystème .NET et IoT, maintient près de 100 projets open source (dont 34 principaux) et fournit des solutions matérielles et logicielles intégrées aux secteurs de l'énergie, de la logistique, de la santé, du contrôle industriel et d'Internet. Notre devise : « l'apprentissage ne connaît pas l'ancienneté — celui qui maîtrise devient maître ».

Les composants open source ont été téléchargés <!--nuget-total:start-->6.48M<!--nuget-total:end--> fois depuis NuGet (à l'échelle de l'écosystème, en date de 2026-10).

Site web : https://newlifex.com · Organisation open source : https://github.com/NewLifeX

<details>
<summary>Compte officiel WeChat : 智能大石头 (scannez pour suivre)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Licence

Ce projet est distribué sous licence [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) ; le certificat de nom fort `newlife.snk` est publié pour que vous puissiez compiler et remplacer vous-même les assemblys, et les sources peuvent être modifiées et redistribuées sans attribution.

> Si ce projet vous est utile, n'hésitez pas à lui donner une Star, à le partager avec votre équipe ou à proposer des améliorations via Issue/PR.

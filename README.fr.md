# NewLife.Core - Bibliothèque principale

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=v12)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)

🌐 [中文](Readme.MD) | [English](README.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [Русский](README.ru.md) | [Português](README.pt.md) | [العربية](README.ar.md)

NewLife est un écosystème complet de composants .NET qui fournit une infrastructure hautes performances pour construire des applications évolutives. Il offre des fonctionnalités essentielles telles que la journalisation, le réseau, la sérialisation, le cache et le multithreading, servant de base à toute une gamme d'applications, des services Web aux appareils IoT.

Tutoriel de la bibliothèque principale : [https://newlifex.com/core](https://newlifex.com/core)

> ⚠️ Cette traduction peut être en retard ; la version de référence est le [README chinois](Readme.MD).



## Vue d'ensemble du framework

NewLife est composé de plusieurs bibliothèques de composants, et NewLife.Core en fournit la base. Le framework prend en charge de multiples frameworks cibles de net45 à net10.0 (de .NET Framework 4.5 à .NET 10.0), ce qui permet une large compatibilité dans différents environnements.

**NativeAOT** : la chaîne principale est prise en charge (25 scénarios testés en conditions réelles + CI anti-régression) ; les capacités dynamiques telles que les plugins et les scripts ne sont pas prises en charge par conception ; les limites sont détaillées dans [Analyse de compatibilité AOT (中文)](Doc/AOT兼容性分析.md).



## Index de la documentation

Le répertoire `Doc/` du dépôt maintient le système de documentation des capacités (définition des exigences → suivi des fonctionnalités → conception de l'architecture) :

- [Documentation des exigences (中文)](Doc/需求文档.md) : vision, objectifs principaux et exigences fonctionnelles
- [Liste des fonctionnalités (中文)](Doc/功能清单.md) : suivi de l'état d'implémentation / de test / de commentaires de tous les points fonctionnels
- [Conception de l'architecture (中文)](Doc/架构设计.md) : couches, composants modulaires et décisions de conception clés
- [Catalogue de la bibliothèque principale (中文)](Doc/核心库目录.md) : index des articles sur les composants principaux
- [Analyse de compatibilité AOT (中文)](Doc/AOT兼容性分析.md) : périmètre de prise en charge NativeAOT, conclusions des tests réels (smoke test 29/29), guide d'utilisation et limites connues

## Démarrage rapide

La façon la plus simple d'utiliser les composants NewLife est de les référencer depuis NuGet, par exemple en recherchant `NewLife.Core` dans le gestionnaire NuGet du projet et en l'ajoutant.

Les composants NewLife sont co-créés par la communauté depuis plus de 20 ans, sous licence open source MIT ; **chacun peut les modifier librement et les redistribuer** (sans avoir à déclarer la source) ! De nombreuses entreprises qui construisent ainsi leur framework de développement interne peuvent même réaliser une personnalisation privatisée en remplaçant en masse, dans le code source, toutes les chaînes `NewLife` par le nom de leur société.

L'équipe adopte toujours une attitude ouverte : le projet se compile non seulement en ouvrant la solution dans VisualStudio (dernière version stable), mais aussi en ligne de commande avec `dotnet build` ; les fichiers de projet rejettent les fonctionnalités complexes au profit de la simplicité d'utilisation, pour une véritable expérience prête à l'emploi.

Nous publions le certificat de nom fort `newlife.snk` afin de permettre la compilation et le remplacement indépendants des assemblys.



Exécutez les commandes suivantes en ligne de commande pour découvrir rapidement les composants NewLife :

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```



## Fonctionnalités principales


| Catégorie de module | Point fonctionnel | Principales caractéristiques |
| ---------------- | ---------------------------------------------------------------------------- | ------------------------------------------------ |
| **Extensions de base** | [Conversion de types Utility](https://newlifex.com/core/utility) | Fournit une conversion de types efficace, avec prise en charge de divers formats et la gestion des exceptions. |
|  | [Extensions de chaînes StringHelper](https://newlifex.com/core/string_helper) | Inclut de nombreuses opérations sur les chaînes, telles que la troncature, l'encodage et le formatage. |
|  | [Extensions de chemins PathHelper](https://newlifex.com/core/path_helper) | Gère intelligemment de multiples formats de chemins pour une compatibilité multiplateforme du code. |
|  | [Extensions de données IOHelper](https://newlifex.com/core/io_helper) | Optimise les opérations d'E/S et fournit des conversions efficaces entre flux de données et tableaux d'octets. |
|  | [Extensions de processus ProcessHelper](https://newlifex.com/core/process_helper) | Simplifie les opérations sur les processus avec des méthodes pratiques pour obtenir et gérer les informations de processus. |
|  | [Informations d'exécution Runtime](https://newlifex.com/core/runtime) | Fournit des méthodes simples pour obtenir les informations d'exécution de l'application, facilitant le débogage et le diagnostic. |
|  | [Objet jetable DisposeBase](https://newlifex.com/core/disposebase) | Fournit un modèle standard de libération des ressources, évitant efficacement les fuites de mémoire et de ressources. |
|  | [Extensions de réflexion Reflect](https://newlifex.com/core/reflect) | Outil de réflexion hautes performances pour lire et écrire rapidement les membres des objets. |
|  | [Extensions de sécurité SecurityHelper](https://newlifex.com/core/security_helper) | Intègre de nombreux algorithmes de chiffrement/déchiffrement tels que RSA/AES/DES/MD5, simplifiant les opérations de sécurité. |
| **Journalisation et traçage** | [Journalisation ILog](https://newlifex.com/core/log) | Prend en charge plusieurs modes de sortie de journal et offre de puissantes fonctions d'écriture asynchrone. |
|  | [Traçage distribué ITracer](https://newlifex.com/core/tracer) | Puissant traçage de performance APM, avec intégration à la plateforme Stardust. |
| **Planification et minutage** | [Minuteur avancé TimerX](https://newlifex.com/core/timerx) | Minuteur haute précision, prenant en charge la planification Cron et l'exécution périodique asynchrone. |
|  | [Expressions Cron](https://newlifex.com/core/cron) | Fournit des tâches planifiées similaires à Linux Cron, avec correspondance à la seconde près. |
|  | [Fournisseur de temps TimeProvider](/core/time_provider) | Abstraction de temps unifiée et calcul de durée haute précision, facilitant la compatibilité entre versions. |
| **Système de configuration** | [Système de configuration Config](https://newlifex.com/core/config) | Framework de configuration unifié, prenant en charge plusieurs formats, plusieurs sources et le rechargement à chaud. |
|  | [Fournisseur de configuration IConfigProvider](/core/config_provider) | Interface de configuration unifiée, avec rappels et modèle de nœuds de configuration. |
|  | [Configuration du noyau Setting](https://newlifex.com/core/setting) | Gère la configuration du noyau des composants, contrôlant des comportements de base comme la journalisation et le réseau. |
|  | [Configuration système SysConfig](https://newlifex.com/core/sysconfig) | Gère la configuration du système applicatif, avec prise en charge des configurations locales, distribuées et chiffrées. |
| **Sérialisation** | [Sérialisation Json](https://newlifex.com/core/json) | Outil Json hautes performances, prenant en charge la sérialisation dans de nombreux scénarios complexes. |
|  | [Sérialisation Xml](https://newlifex.com/core/xml) | Fournit la sérialisation et la désérialisation XML, avec conversion bidirectionnelle entre objets et XML. |
|  | [Sérialisation binaire](https://newlifex.com/core/binary) | Sérialisation binaire hautes performances, prenant en charge les graphes d'objets complexes et le versionnage. |
|  | [Sérialisation binaire avancée](/core/binary_advanced) | Guide avancé pour l'encodage/décodage de bout en bout sans allocation des messages de protocole. |
|  | [Analyse Csv CsvFile](https://newlifex.com/core/csv_file) | Lecture et écriture rapides de fichiers CSV, avec prise en charge de séparateurs personnalisés et de la liaison de données. |
|  | [Base de données Csv CsvDb](https://newlifex.com/core/csv_db) | Utilise des fichiers CSV comme base de données, avec prise en charge des opérations de base d'ajout, de suppression, de modification et de consultation. |
|  | [Lecteur Excel léger ExcelReader](https://newlifex.com/core/excel_reader) | Lecture rapide d'Excel sans dépendance à Office, prenant en charge de gros volumes de données. |
| **Cache** | [Interface de cache unifiée ICache](https://newlifex.com/core/icache) | Définit des opérations de cache standard et fournit de nombreuses implémentations : mémoire, Redis, etc. |
|  | [Architecture de cache ICacheProvider](https://newlifex.com/core/icacheprovider) | Sert à gérer et fournir des instances ICache, avec prise en charge de la coexistence de plusieurs caches. |
|  | [Cache mémoire MemoryCache](https://newlifex.com/core/memory_cache) | Cache mémoire local hautes performances, avec stratégies d'expiration et de capacité. |
|  | [Pool d'objets ObjectPool](https://newlifex.com/core/object_pool) | Permet de réutiliser des objets coûteux, améliorant les performances et réduisant la pression sur le GC. |
|  | [Pool d'objets Pool](/core/pool) | Pool d'objets générique hautes performances basé sur CAS, réduisant la pression sur le GC. |
|  | [Cache distribué NewLife.Redis](https://newlifex.com/core/redis) | Implémentation de cache distribué basée sur Redis, prenant en charge diverses structures de données. |
|  | [Stockage vectoriel IVectorStore](/core/vector_store) | Interface de gestion de collections vectorielles, avec recherche de similarité Top-K, fournissant une infrastructure de recherche vectorielle pour les applications d'IA. |
| **Tampons hautes performances** | [Paquet de données IPacket](https://newlifex.com/core/packet) | Interface de paquet de données centrale pour le découpage sans copie et les paquets chaînés. |
|  | [Tampons Buffers](/core/buffers) | Bibliothèque d'opérations de tampons hautes performances, regroupant des composants tels que Span et l'écriture mise en pool. |
|  | [Écrivain mis en pool PooledByteBufferWriter](/core/pooled_buffer_writer) | Écrivain de tampon mis en pool basé sur ArrayPool, avec extension dynamique de la capacité. |
|  | [Lecteur Span SpanReader](/core/span_reader) | Analyseur de flux binaire hautes performances sans allocation, avec prise en charge des formats petit-boutiste et gros-boutiste. |
|  | [Écrivain Span SpanWriter](/core/span_writer) | Écrivain de données binaires hautes performances sans allocation, avec prise en charge des formats petit-boutiste et gros-boutiste. |
|  | [Assistant Span SpanHelper](/core/span_helper) | Ensemble de méthodes d'extension Span/ReadOnlySpan, simplifiant les opérations sur la mémoire. |
|  | [Lecteur séquentiel SequenceReader](/core/sequence_reader) | Lecteur séquentiel multi-segments (anciens frameworks complétés par des shims, disponible sur les 18 TFM). |
| **Framework applicatif** | [Conteneur d'objets ObjectContainer](https://newlifex.com/core/object_container) | Injection de dépendances légère, avec prise en charge de multiples durées de vie et modes d'injection. |
|  | [Hôte d'application léger Host](https://newlifex.com/core/host) | Simplifie l'hébergement des applications pour construire facilement des services d'arrière-plan de longue durée. |
|  | [Framework de plugins IPlugin](https://newlifex.com/core/plugin) | Fournit un mécanisme standardisé de gestion des plugins pour un développement applicatif modulaire. |
|  | [Modèle de parallélisme Actor](https://newlifex.com/core/actor) | Modèle de programmation concurrente simplifié, augmentant le débit grâce au traitement par file de messages. |
|  | [Modèle de pipeline Pipeline](/core/pipeline) ⚠️ Retiré, remplacé par le modèle de protocole (pompe de trames) | Modèle de chaîne de traitement bidirectionnelle, unifiant les flux Read/Write/Open/Close. |
|  | [File différée DeferredQueue](/core/deferred_queue) | Agrège les modifications à haute fréquence et les persiste par lots, réduisant l'amplification d'écriture. |
|  | [Bus d'événements EventBus](/core/eventbus) | Distribution d'événements intra-processus, avec routage par sujet et messages en file d'attente. |
|  | [Délégué à référence faible WeakAction](https://newlifex.com/core/weak_action) | Résout les fuites de mémoire liées aux abonnements d'événements pour une gestion sûre des événements. |
|  | [Moteur de scripts ScriptEngine](https://newlifex.com/core/script_engine) | Prend en charge la compilation et l'exécution dynamiques de C#, offrant une capacité d'extension par scripts flexible. |
|  | [Compression et décompression](https://newlifex.com/core/compression) | Intègre de nombreux algorithmes de compression avec une interface unifiée pour compresser les données. |
|  | [Archives compressées Tar et 7Zip](/core/tar_7zip) | Couvre les usages avancés d'archivage et de compression Tar/Tar.Gz et 7Zip. |
|  | [Informations machine MachineInfo](https://newlifex.com/core/machine_info) | Obtient de manière exhaustive les informations matérielles, sous Windows et Linux. |
|  | [Bibliothèque PinYin](https://newlifex.com/core/pinyin) | Bibliothèque efficace de conversion des caractères chinois en pinyin, avec pinyin complet et abrégé, aux performances supérieures. |
|  | [Jeu de données DbTable](https://newlifex.com/core/dbtable) | Table de données en mémoire, facilitant le traitement et l'analyse des données. |
|  | [Paramètres de pagination PageParameter](https://newlifex.com/core/page_parameter) | Encapsule la logique de pagination, prenant en charge divers scénarios de pagination (Web, API, etc.). |
|  | [Algorithme Snowflake](https://newlifex.com/core/snow_flake) | Algorithme de génération d'identifiants uniques distribués, garantissant l'unicité des ID sous forte concurrence. |
| **Sécurité et jetons** | [Jeton à signature numérique distribuée TokenProvider](https://newlifex.com/core/token_provider) | Sert à générer et vérifier des jetons distribués, garantissant la sécurité entre plusieurs nœuds. |
|  | [Jeton Web générique JwtBuilder](https://newlifex.com/core/jwt) | Fournit la génération, le décodage et la vérification des jetons JWT, simplifiant la validation Web. |
|  | [Filtre HTTP de jetons TokenHttpFilter](https://newlifex.com/core/token_http_filter) | Injecte automatiquement le jeton dans les requêtes HTTP et renouvelle automatiquement le jeton en interceptant les 401/403. |
| **Services cloud** | [Hachage latitude/longitude GeoHash](https://newlifex.com/core/geo_hash) | Encode latitude et longitude sous forme de chaîne de caractères, pour la recherche de positions à proximité. |
|  | ⚠️ Baidu Maps / Amap ont été extraits dans un package NuGet indépendant. |  |
| **Bibliothèque réseau** | [Serveur réseau NetServer](https://newlifex.com/core/netserver) | Serveur TCP/UDP/socket de domaine Unix hautes performances, encapsulant les sessions et le traitement des données. |
|  | [Client réseau NetClient](/core/netclient) | Client réseau unifié TCP/UDP/Unix, simplifiant la connexion et les échanges. |
|  | [Message IMessage](/core/message) | Contrat de message requête-réponse, portant la sémantique de direction et la lecture/écriture de trames. |
|  | [Pile de protocole de messages MessagePump](/core/message_protocol) | Pile unique pompe de trames + codec sans état, prenant en charge les corps en flux de grandes trames et la contre-pression. |
|  | [Pipeline de données Pipe](/core/packet_pipe) | Pipeline de données sans copie, aligné sur la forme de System.IO.Pipelines. |
|  | [Protocole simple d'échange de messages à distance SRMP](https://newlifex.com/core/srmp) | Protocole RPC léger, prenant en charge la sérialisation binaire et le transport UDP. |
|  | [Équilibrage de charge et basculement LoadBalancer](/core/load_balancer) | Fournit trois stratégies de planification multi-nœuds : Failover, tourniquet et course. |
|  | [Communication RPC ApiServer](https://newlifex.com/core/api_server) | ⚠️ Déplacé vers [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
|  | [Communication RPC ApiClient](https://newlifex.com/core/api_client) | ⚠️ Déplacé vers [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
|  | [Client HTTP ApiHttpClient](https://newlifex.com/core/api_http) | Encapsulation basée sur HttpClient, simplifiant l'appel et le traitement des API Web. |
|  | [Serveur HTTP HttpServer (appareils embarqués)](https://newlifex.com/core/httpserver) | Serveur Http léger, prenant en charge le pipeline de middlewares, le routage paramétré et le service de fichiers statiques. |
|  | [Téléchargement réseau WebClientX](https://newlifex.com/core/webclient) | Version améliorée de WebClient, avec téléchargement multithread et reprise après interruption. |
|  | [Serveur WebSocket (outil idéal pour les notifications descendantes IoT)](https://newlifex.com/core/websocket) | Serveur WebSocket hautes performances, adapté aux notifications de messages IoT. |
|  | [Communication bidirectionnelle WebSocket](/core/websocket_guide) | Présente les pratiques de handshake, de traitement des trames, de heartbeat et de fermeture des connexions. |
|  | [Client de stockage de fichiers EasyClient](/core/easy_io) | Client de stockage de fichiers HTTP, compatible avec EasyIO ou OSS. |



## Matrice des projets NewLife
Par défaut, chaque projet prend en charge net10.0/netstandard2.1/netstandard2.0/net4.62/net4.5 ; les anciennes versions (2024.0801) prennent en charge net4.0/net2.0

| Projet | Année | Description |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
| Composants de base |  | Soutient les autres intergiciels et les projets produits |
| [NewLife.Core](https://github.com/NewLifeX/X) | 2002 | Bibliothèque principale : journalisation, configuration, cache, réseau, sérialisation, traçage de performance APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 2005 | Intergiciel big data, tables uniques de l'ordre de dizaines de milliards de lignes, MySql/SQLite/SqlServer/Oracle/PostgreSql/DaMeng, partitionnement automatique de tables, séparation lecture/écriture |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 2005 | Bibliothèque réseau, débit de l'ordre de dizaines de millions par serveur (22,66 millions tps), millions de connexions par serveur (4 millions de connexions TCP persistantes) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011 | Bibliothèque de communication par protocoles, fournit un framework de communication pour applications C/S, prend en charge les frameworks de communication Http/RPC, haut débit, accès facile et à faible coût pour les appareils IoT |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 2010 | Plateforme de développement rapide Cube, intégrant les permissions utilisateur, la connexion SSO, le serveur OAuth, etc., validée par des projets à 10 milliards de lignes par table |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 2008 | Composant de gestion de services, installe l'application comme démon du système d'exploitation : service Windows, Systemd sous Linux |
| [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero) | 2020 | Échafaudage Zero, modèles de projet NewLife.Templates basés sur l'écosystème de composants NewLife : Web, WebApi, Service |
| Intergiciels |  | Connexion à des plateformes d'intergiciels connues |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | 2017 | Client Redis, latence de l'ordre de la microseconde, débit de l'ordre du million, files de messages riches, validé par des projets à des dizaines de milliards de données |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018 | Client RocketMQ entièrement managé, prend en charge Apache RocketMQ et la file de messages d'Alibaba Cloud, validé par des projets à l'échelle du milliard |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | 2019 | Protocole de messagerie IoT, MqttClient/MqttServer, le client prend en charge Alibaba Cloud IoT |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 2022 | Bibliothèque standard IoT, définissant les normes et spécifications des divers protocoles de communication du domaine IoT |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 2022 | ModbusTcp/ModbusRTU/ModbusASCII, implémenté sur la bibliothèque standard IoT, prend en charge la plateforme ZeroIoT et la passerelle IoTEdge |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 2022 | Protocole PLC Siemens, implémenté sur la bibliothèque standard IoT, prend en charge la plateforme IoT et IoTEdge |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 2022 | Bibliothèque de composants cartographiques, encapsulant Baidu Maps, Amap, Tencent Maps et Tianditu |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 2023 | Bibliothèque de codecs audio : PCM/ADPCMA/G711A/G722U/WAV/AAC |
| Plateformes produits |  | Niveau plateforme produit : prêt à l'emploi après compilation et déploiement, personnalisable |
| [Stardust](https://github.com/NewLifeX/Stardust) | 2018 | Stardust, plateforme de services distribués : gestion de nœuds, centre de supervision APM, centre de configuration, centre d'enregistrement, centre de publication |
| [AntJob](https://github.com/NewLifeX/AntJob) | 2019 | AntJob, planificateur de calcul big data distribué (temps réel/hors ligne), approche de fragmentation par « déménagement de fourmi », validée par des projets à l'échelle du billion de données |
| [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP) | 2021 | ERP d'entreprise : gestion des produits, gestion des clients, gestion des ventes, gestion des fournisseurs |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 2006 | Outil CrazyCoder, nombreux outils pour développeurs : réseau, port série, chiffrement/déchiffrement, expressions régulières, Modbus, MQTT |
| [EasyIO](https://github.com/NewLifeX/EasyIO) | 2023 | Stockage de fichiers simplifié, prend en charge le stockage centralisé des fichiers dans les systèmes distribués. |
| [XProxy](https://github.com/NewLifeX/XProxy) | 2005 | Proxy inverse de niveau produit : proxy NAT, proxy Http |
| [HttpMeter](https://github.com/NewLifeX/HttpMeter) | 2022 | Outil de test de charge Http |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 2015 | Système de gestion de code source Git |
| [SmartOS](https://github.com/NewLifeX/SmartOS) | 2014 | Système d'exploitation embarqué, entièrement autonome, prenant en charge l'architecture de puces ARM Cortex-M |
| [SmartA2](https://github.com/NewLifeX/SmartA2) | 2019 | Ordinateur industriel embarqué, passerelle de périphérie IoT, hôte .NET8 hautes performances, utilisé dans l'industrie, l'agriculture, les transports et la santé |
| Plateforme IoT FIoT | 2020 | Solution globale IoT : bâtiment, protection de l'environnement, agriculture ; intégration du logiciel, du matériel et de l'analyse big data ; validée par des projets à cent mille points par serveur |



---
# Équipe de développement NewLife
![XCode](https://newlifex.com/logo.png)

L'équipe NewLife (新生命) a été fondée en 2002. Elle est un fournisseur de solutions pour l'industrie de l'Internet des objets de la nouvelle ère, dédiée au conseil en solutions logicielles et matérielles, à la planification d'architecture système et aux services de développement. Plus de 5,23 millions de téléchargements cumulés sur NuGet. Les composants principaux de l'équipe (XCode / Redis / Stardust / AntJob / IoT, etc.) sont déployés à grande échelle dans l'électricité, l'industrie, les transports, la protection de l'environnement, la santé et le patrimoine culturel.

Nous nous améliorons continuellement, afin de devenir un partenaire technologique .NET / IoT / distribué digne d'une confiance durable.

Site web : https://newlifex.com   Organisation open source : https://github.com/NewLifeX  
Compte officiel WeChat :  
![智能大石头](https://newlifex.com/stone.jpg)

---
> Si ce projet vous est utile, vous êtes invités à nous donner une Star, à le partager avec votre équipe, ou à nous faire part de vos suggestions d'amélioration via Issue/PR.

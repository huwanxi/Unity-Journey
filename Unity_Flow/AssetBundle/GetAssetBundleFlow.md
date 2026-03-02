flowchart TD
    A[调用GetAssetBundle<br/>url + hash + crc] --> B{查缓存表<br/>是否有 (BundleName,Hash)}
    B -->|有| C[直接从缓存加载<br/>（不再校验CRC）]
    B -->|无| D[从服务器下载]
    D --> E[下载完成]
    E --> F{计算下载文件的CRC}
    F -->|与传入的CRC一致| G[✅ 文件完整<br/>存入缓存]
    F -->|与传入的CRC不一致| H[❌ 文件损坏<br/>丢弃文件]

    G --> I[返回AssetBundle]
    H --> J[返回DataProcessingError]

    C --> I

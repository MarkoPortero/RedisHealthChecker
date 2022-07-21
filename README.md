**What it does**<br>
This console app will perform diagnostics on Redis servers.

Both the Ping Test and the Read/Write test will be performed automatically. These _should_ highlight any issues. <br>
Further diagnostics are available via the Cluster Info/Cluster Nodes test.

**Ping test**<br>
All servers get pinged to check for any possible VM issues

**Read/Write Test**<br>
The console app will attempt to read/write from all specified redis servers

**Cluster Info Test**<br>
The CLUSTER INFO command will be called on the servers. All servers will be tested.<br>
Any servers that report back with a result other than 'cluster_state:ok', or with a fail/pfail of greater than 0 will be mentioned at the end of the test.<br>
More info can be found here: https://redis.io/commands/cluster-info

**Cluster Nodes Test**<br>
The CLUSTER NODES command will be called on the servers. All servers will be tested.<br>
Any servers that report back with 'fail' or 'fail?' will be mentioned at the end of the test.<br>

More info can be found here: https://redis.io/commands/cluster-nodes

**Redis Repair**<br>
This option will be offered if servers report as being down. Executing this will connect to any downed servers and execute a shutdown/restart on each of the downed servers.

If this step fails to fix the issue, the exe will offer to fix broken configs. This will blow away Redis on the servers that are down and reform it.

**How to use**<br>
Download, build, run.<br>
If net5.0 is not locally available, you can retarget the TargetFramework in the csproj to an earlier .netcore version<br>

IPs are currently in the RedisValues class. If changing, make sure to follow the pattern of IP:PORT.<br>
New additions should be added to the _addresses array on program.cs

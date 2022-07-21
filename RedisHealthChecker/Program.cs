namespace RedisHealthChecker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using Renci.SshNet;
    using StackExchange.Redis;

    public class Program
    {
        private static string _redisPassword = "Password123!";
        private static readonly List<string> _failureSetList = new();
        private static readonly List<string> _failureAddressList = new();
        private static ConnectionMultiplexer _connectionMultiplexer;
        private static string _sshUser = "root";
        private static string _sshPassword = "Password123";
        private static readonly string[] _addresses = {
            RedisValues.DevRedis1, RedisValues.DevRedis2, RedisValues.DevRedis3

        private static readonly string[] _devAddresses =
        {
            RedisValues.DevRedis1, RedisValues.DevRedis2, RedisValues.DevRedis3
        };

        static void Main()
        {
            string[] addresses = _devAddresses;

            HandleRedisPing(addresses);
            HandleRedisReadWrite(addresses);
            HandleRedisClusterInfoCall(addresses);
            HandleRedisClusterNodesCall(addresses);

            RepairRedis();
            RepairRedisConfig();

            Console.WriteLine("\nCompleted Redis Diagnostics.");
            Console.ReadLine();
        }

            public int[] TwoSum(int[] nums, int target)
            {
                foreach (var num in nums)
                {
                    foreach (var num2 in nums)
                    {
                        var result = num + num2;
                        if (result == target)
                        {
                            int[] array = new int[2];
                            array[0] = Array.IndexOf(nums, num);
                            array[1] = Array.IndexOf(nums, num2);
                            return array;
                        }
                    }
                }
                return null;
            }
        
        private static void RepairRedisConfig()
        {
            if (_failureAddressList.Any())
            {
                Console.WriteLine("\nCluster is still down. Repair configs on failed servers? Y/N");
                var performRedisClusterFix = Console.ReadLine();

                try
                {
                    if (performRedisClusterFix != null && Convert.ToChar(performRedisClusterFix.ToLower()) == 'y')
                    {
                        if (_failureAddressList.Intersect(_devAddresses).Any())
                        {
                            HandleConfigRepair(_devAddresses);
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static void HandleConfigRepair(string[] addresses)
        {
            ShutdownRedis(addresses);
            Thread.Sleep(200);
            DeleteRedisNodeConfigs(addresses);
            Thread.Sleep(200);
            RecreateCluster(addresses);
            Thread.Sleep(200);
            JoinClusterNodes(addresses);
            Console.WriteLine("\nChecking if cluster repair worked..");
            _failureSetList.Clear();
            HandleRedisPing(_failureAddressList.ToArray());
            HandleRedisReadWrite(_failureAddressList.ToArray());
        }

        private static string GetSshPassword(string ip)
        {
            switch (ip)
            {
                default:
                    return _sshPassword;
            }
        }

        private static void JoinClusterNodes(string[] addresses)
        {
            var server1 = addresses[0].Split(':')[0];
            var server2 = addresses[1].Split(':')[0]; ;
            var server3 = addresses[2].Split(':')[0]; ;

            try
            {
                var ip = server1;
                SshClient sshclient = new(ip, _sshUser, GetSshPassword(ip));
                sshclient.Connect();
                var recreateClusterCommand = sshclient.CreateCommand($@"redis-cli --cluster create \{server1}:7000 {server1}:7001 \{server1}:7002 {server1}:7004 \{server1}:7003 {server1}:7005 \{server2}:7000 {server2}:7001 \{server2}:7002 {server2}:7004 \{server2}:7003 {server2}:7005 \{server3}:7000 {server3}:7001 \{server3}:7002 {server3}:7004 \{server3}:7003 {server3}:7005 \-a Password123! --cluster-replicas 1 --cluster-yes");
                recreateClusterCommand.Execute();
                Console.WriteLine($"{ip} - {recreateClusterCommand.Result}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error trying to recreate cluster: {e}");
            }
        }

        private static void RecreateCluster(string[] addresses)
        {
            foreach (var address in addresses)
            {
                try
                {
                    var ip = address.Split(':')[0];
                    SshClient sshclient = new(ip, _sshUser, GetSshPassword(ip));
                    sshclient.Connect();
                    var recreateClusterCommand = sshclient.CreateCommand($"cd /etc/redis/ && ./create_redis_cluster.sh");
                    recreateClusterCommand.Execute();
                    Console.WriteLine($"{ip} - {recreateClusterCommand.Result}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error trying to recreate cluster: {e}");
                }
            }
        }

        private static void DeleteRedisNodeConfigs(string[] addresses)
        {
            foreach (var address in addresses)
            {
                try
                {
                    var ip = address.Split(':')[0];
                    SshClient sshclient = new(ip, _sshUser, GetSshPassword(ip));
                    sshclient.Connect();

                    for (var x = 7000; x < 7005; x++)
                    {
                        var configDeleteCommand = sshclient.CreateCommand($"cd /etc/redis/{x} && rm -f nodes.conf");
                        configDeleteCommand.Execute();
                        Console.WriteLine($"{ip} - {configDeleteCommand.Result}");
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error trying to delete node configs: {e}");
                }
            }
        }

        private static void ShutdownRedis(string[] addresses)
        {
            foreach (var address in addresses)
            {
                try
                {
                    var ip = address.Split(':')[0];
                    SshClient sshclient = new(ip, _sshUser, GetSshPassword(ip));
                    sshclient.Connect();

                    var redisStopCmd = sshclient.CreateCommand("cd /etc/redis/ && ./stop_redis_cluster.sh");
                    redisStopCmd.Execute();
                    Console.WriteLine($"{ip} - {redisStopCmd.Result}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error trying to shutdown redis: {e}");
                }
            }
        }

        private static void HandleRedisClusterNodesCall(string[] addresses)
        {
            Console.WriteLine("\nPerform CLUSTER NODES on servers? Y/N");
            var performClusterNodes = Console.ReadLine();
            try
            {
                if (performClusterNodes != null && Convert.ToChar(performClusterNodes.ToLower()) == 'y')
                {
                    RunClusterNodes(addresses);
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void HandleRedisClusterInfoCall(string[] addresses)
        {
            Console.WriteLine("\nPerform CLUSTER INFO on servers? Y/N");
            var performClusterInfo = Console.ReadLine();
            try
            {
                if (performClusterInfo != null && Convert.ToChar(performClusterInfo.ToLower()) == 'y')
                {
                    RunClusterInfo(addresses);
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void HandleRedisPing(string[] addresses)
        {
            Console.WriteLine("Attempting to ping Redis...\n");
            PingRedis(addresses);
        }

        private static void HandleRedisReadWrite(string[] addresses)
        {
            Console.WriteLine("\nAttempting to write and get from Redis...\n");
            SetGetRedis(addresses);
            if (_failureSetList.Any())
            {
                Console.WriteLine("The servers that failed: ");
                _failureSetList.ForEach(Console.WriteLine);
            }
            else
            {
                Console.WriteLine("\nNo servers failed Redis write/read test!");
            }
        }

        private static void SetGetRedis(string[] addresses)
        {
            foreach (var address in addresses)
            {
                CheckRedis(address);
            }
        }

        private static void PingRedis(string[] addresses)
        {
            var pingSender = new Ping();
            var options = new PingOptions
            {
                DontFragment = true
            };

            var data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var buffer = Encoding.ASCII.GetBytes(data);
            var timeout = 120;
            var failureList = new List<string>();
            foreach (var address in addresses)
            {
                string[] ips = address.Split(':');
                var serverName = GetServerName(address);
                var retry = 0;
                try
                {
                    var reply = pingSender.Send(ips[0], timeout, buffer, options);

                    while (reply.Status == IPStatus.TimedOut && retry != 5)
                    {
                        Thread.Sleep(50);
                        reply = pingSender.Send(ips[0], timeout, buffer, options);
                        retry++;
                    }

                    if (reply.Status != IPStatus.Success)
                    {
                        failureList.Add(serverName);
                    }

                    Console.WriteLine($"Server: {serverName} Address: {reply.Address}, status = {reply.Status}");
                }
                catch
                {
                    failureList.Add(serverName);
                    Console.WriteLine($"Server: {serverName} Address: {address}, status = Failure");
                }
            }

            if (failureList.Any())
            {
                Console.WriteLine($"The following servers have failed to respond to ping: ");
                failureList.ForEach(Console.WriteLine);
                return;
            }

            Console.WriteLine("\nAll servers responding to ping.");
        }

        private static void CheckRedis(string address)
        {
            var serverName = GetServerName(address);
            try
            {
                var database = SetupRedisConnection(address);
                var keyName = Guid.NewGuid().ToString();
                var inputValue = Guid.NewGuid().ToString();
                database.StringSet(keyName, inputValue, TimeSpan.FromSeconds(10));
                var value = database.StringGet(keyName);

                if (value.ToString() != inputValue)
                {
                    Console.WriteLine($"Server: {serverName}:{address} Redis status check: Unavailable.");
                    return;
                }

                Console.WriteLine($"Server: {serverName}:{address} Redis status check: Available");
                _connectionMultiplexer.Dispose();
            }
            catch
            {
                Console.WriteLine($"Server: {serverName}:{address} Redis status check: Unavailable.");
                _failureSetList.Add(serverName);
                _failureAddressList.Add(address);
            }

        }

        private static string GetServerName(string address)
        {
            var props = typeof(RedisValues).GetFields(BindingFlags.Public | BindingFlags.Static);
            var name = props.FirstOrDefault(prop => (string)prop.GetValue(null) == address);
            var serverName = name.Name;
            return serverName;
        }

        private static IDatabase SetupRedisConnection(string address)
        {
            var options = ConfigurationOptions.Parse(address);
            if (address.Contains("10.200.203.201"))
            {
                _redisPassword = "baf0da7b60589ce596de0422909f180d";
            }
            
            options.Password = _redisPassword;
            options.AllowAdmin = true;
            _connectionMultiplexer = ConnectionMultiplexer.Connect(options);
            var database = _connectionMultiplexer.GetDatabase();
            _connectionMultiplexer.GetServer(address);
            return database;
        }

        private static void RunClusterInfo(string[] addresses)
        {
            List<string> failures = new List<string>();
            Console.WriteLine("\nRunning CLUSTER INFO on servers...\n");
            foreach (var address in addresses)
            {
                try
                {
                    SetupRedisConnection(address);
                    var redisResult = _connectionMultiplexer.GetDatabase().Execute("CLUSTER", "INFO").ToString();
                    Console.WriteLine($"CLUSTER INFO for {GetServerName(address)} {address}");
                    if (!redisResult.Contains("cluster_state:ok") ||
                        !redisResult.Contains("cluster_slots_pfail:0") ||
                        !redisResult.Contains("cluster_slots_fail:0"))
                    {
                        failures.Add(GetServerName(address));
                    }
                    Console.WriteLine(redisResult);
                    _connectionMultiplexer.Dispose();
                }
                catch
                {
                    Console.WriteLine($"{address} Error connecting to Redis server or executing command.");
                }
            }

            Console.WriteLine("Completed running CLUSTER INFO on servers.");

            if (failures.Any())
            {
                Console.WriteLine($"Possible cluster issues with:");
                failures.ForEach(Console.WriteLine);
                return;
            }

            Console.WriteLine("No immediate issues detected from CLUSTER INFO.");
        }

        private static void RunClusterNodes(string[] addresses)
        {
            List<string> failures = new List<string>();
            Console.WriteLine("\nRunning CLUSTER NODES on servers...\n");
            foreach (var address in addresses)
            {
                try
                {
                    SetupRedisConnection(address);
                    var server = _connectionMultiplexer.GetServer(address);
                    Console.WriteLine($"CLUSTER NODES for {GetServerName(address)} {address}");
                    var clusterNodesRaw = server.ClusterNodesRaw();
                    if (clusterNodesRaw.Contains("fail?") || clusterNodesRaw.Contains("fail"))
                    {
                        failures.Add(GetServerName(address));
                    }
                    Console.WriteLine(clusterNodesRaw);
                    _connectionMultiplexer.Dispose();
                }
                catch
                {
                    Console.WriteLine($"{address} Error connecting to Redis server or executing command.");
                }
            }

            Console.WriteLine("Completed running CLUSTER NODES on servers.");

            if (failures.Any())
            {
                Console.WriteLine($"Possible cluster issues with:");
                failures.ForEach(Console.WriteLine);
                return;
            }

            Console.WriteLine("No immediate issues detected from CLUSTER NODES.");

        }

        private static void RepairRedis()
        {
            if (_failureAddressList.Any())
            {
                Console.WriteLine("\nRepair Redis on failed servers? Y/N");
                var performRedisRestart = Console.ReadLine();
                try
                {
                    if (performRedisRestart != null && Convert.ToChar(performRedisRestart.ToLower()) == 'y')
                    {
                        RestartRedisCluster();
                        Thread.Sleep(200);
                        Console.WriteLine("\nChecking if repair worked..");
                        _failureSetList.Clear();
                        HandleRedisPing(_failureAddressList.ToArray());
                        HandleRedisReadWrite(_failureAddressList.ToArray());
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static void RestartRedisCluster()
        {
            foreach (var address in _failureAddressList)
            {
                try
                {
                    var ip = address.Split(':')[0];
                    SshClient sshclient = new(ip, _sshUser, GetSshPassword(ip));
                    sshclient.Connect();

                    var redisStopCmd = sshclient.CreateCommand("cd /etc/redis/ && ./stop_redis_cluster.sh");
                    redisStopCmd.Execute();
                    Console.WriteLine($"{ip} - {redisStopCmd.Result}");

                    Console.WriteLine("\nExecuting Redis Cluster start...");
                    var redisStartCmd = sshclient.CreateCommand("cd /etc/redis/ && ./start_redis_cluster.sh");
                    redisStartCmd.Execute();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error trying to repair redis: {e}");
                }
            }
        }
    }
}

using Azure_Serverless_SignalR_Core_Emulator.Models;
using ConcurrentCollections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azure_Serverless_SignalR_Core_Emulator.Services
{
    public class MappingService
    {
        private ConcurrentDictionary<string, ConnectionMapping> connectionIdToMapping = new ConcurrentDictionary<string, ConnectionMapping>();
        private ConcurrentDictionary<string, ConcurrentHashSet<ConnectionMapping>> hubNameToMappings = new ConcurrentDictionary<string, ConcurrentHashSet<ConnectionMapping>>();
        private ConcurrentDictionary<(string hubName, string userId), ConcurrentHashSet<ConnectionMapping>> hubNameUserIdToMappings = new ConcurrentDictionary<(string hubName, string userId), ConcurrentHashSet<ConnectionMapping>>();
        private ConcurrentDictionary<(string hubName, string group), ConcurrentHashSet<ConnectionMapping>> hubNameGroupToMappings = new ConcurrentDictionary<(string hubName, string group), ConcurrentHashSet<ConnectionMapping>>();

        public List<string> GetConnectionIds(string hubName)
        {
            return hubNameToMappings.GetValueOrDefault(hubName)?.Select(x => x.connectionId).ToList();
        }
        public List<string> GetConnectionIdsWithUserId(string hubName, string userId)
        {
            return hubNameUserIdToMappings.GetValueOrDefault((hubName, userId))?.Select(x => x.connectionId).ToList();
        }
        public List<string> GetConnectionIdsWithGroup(string hubName, string group)
        {
            return hubNameGroupToMappings.GetValueOrDefault((hubName, group))?.Select(x => x.connectionId).ToList();
        }


        public void AddConnection(string connectionId, string hubName, string userId = null)
        {
            var mapping = new ConnectionMapping
            {
                connectionId = connectionId,
                hubName = hubName,
                userId = userId
            };
            connectionIdToMapping.AddOrUpdate(connectionId, mapping, (key, _) => mapping);

            hubNameToMappings.AddOrUpdate(hubName, (_) => {
                var tmp = new ConcurrentHashSet<ConnectionMapping>();
                tmp.Add(mapping);
                return tmp;
            }, (_, hashSet) =>
            {
                hashSet.Add(mapping);
                return hashSet;
            });

            if (userId != null)
            {
                hubNameUserIdToMappings.AddOrUpdate((hubName, userId), (_) =>
                {
                    var tmp = new ConcurrentHashSet<ConnectionMapping>();
                    tmp.Add(mapping);
                    return tmp;
                }, (_, hashSet) =>
                {
                    hashSet.Add(mapping);
                    return hashSet;
                });
            }

        }

        public void RemoveConnection(string connectionId)
        {
            ConnectionMapping mapping;
            if (!connectionIdToMapping.TryRemove(connectionId, out mapping))
                return;
            

            ConcurrentHashSet<ConnectionMapping> connectionMappings;
            if (hubNameToMappings.TryGetValue(mapping.hubName, out connectionMappings) && connectionMappings.TryRemove(mapping) && connectionMappings.Count == 0)
                hubNameToMappings.TryRemove(mapping.hubName, out _);



            if (mapping.userId != null && hubNameUserIdToMappings.TryGetValue((mapping.hubName, mapping.userId), out connectionMappings) && connectionMappings.TryRemove(mapping) && connectionMappings.Count == 0)
                hubNameUserIdToMappings.TryRemove((mapping.hubName, mapping.userId), out _);
            
            if (mapping.groups != null)
            {
                foreach (string group in mapping.groups)
                {
                    if (hubNameGroupToMappings.TryGetValue((mapping.hubName, group), out connectionMappings))
                        connectionMappings.TryRemove(mapping);
                }
            }
        }

        public bool AddToGroup(string hubName, string userId, string group)
        {
            ConcurrentHashSet<ConnectionMapping> mappings;
            if (!hubNameUserIdToMappings.TryGetValue((hubName, userId), out mappings))
                return false;
            foreach (var mapping in mappings)
            {
                if (mapping.groups == null)
                    mapping.groups = new ConcurrentHashSet<string>();
                mapping.groups.Add(group);
                hubNameGroupToMappings.AddOrUpdate((hubName, group), (_) =>
                {
                    var tmp = new ConcurrentHashSet<ConnectionMapping>();
                    tmp.Add(mapping);
                    return tmp;
                }, (_, hashSet) =>
                {
                    hashSet.Add(mapping);
                    return hashSet;
                });
            }
            return true;

        }
        public bool RemoveFromGroup(string hubName, string userId, string group)
        {
            ConcurrentHashSet<ConnectionMapping> hubNameUserIdMappings;
            ConcurrentHashSet<ConnectionMapping> hubNameGroupMappings;
            if (!hubNameUserIdToMappings.TryGetValue((hubName, userId), out hubNameUserIdMappings))
                return false;
            if (!hubNameGroupToMappings.TryGetValue((hubName, group), out hubNameGroupMappings))
                return false;
            var matchMappings = hubNameUserIdMappings.Where(x => hubNameGroupMappings.Contains(x));
            foreach (var mapping in matchMappings)
            {
                if (mapping.groups.TryRemove(group) && mapping.groups.Count == 0)
                    mapping.groups = null;
                if (hubNameGroupMappings.TryRemove(mapping) && hubNameGroupMappings.Count == 0)
                    hubNameGroupToMappings.TryRemove((hubName, group), out _);
            }
            return true;
        }
    }
}

﻿namespace Dumperv2
{
    internal class Jsons
    {
        public class prodconf
        {
            public uint ProductId { get; set; }
            public string Configuration { get; set; }
        }

        public class prodmanifests
        {
            public uint ProductId { get; set; }
            public List<string> Manifest { get; set; } = new();
        }

        public class prodserv
        {
            public uint ProductId { get; set; }
            public string SpaceId { get; set; }
            public string AppId { get; set; }
        }

        public class storeconf
        {
            public uint ProductId { get; set; }
            public string StoreRef { get; set; }
            public Uplay.Store.StorePartner Partner { get; set; }
        }
        public class idmap
        {
            public uint ProductId { get; set; }
            public string Brand { get; set; }
        }

        public class OW
        {
            public uint ProductId { get; set; }
            public string ProductType { get; set; }
            public List<uint> ProductAssociations { get; set; }
        }
    }
}

﻿using System.Collections.Generic;

namespace PubSubDataConstructor.Repositories
{
    public class NullRepository : IRepository
    {
        public void Add(DataCandidate candidate) { }

        public IEnumerable<DataCandidate> List()
        {
            return new DataCandidate[0];
        }

        public IEnumerable<DataCandidate> List(Topic topic)
        {
            return List();
        }

        public void Clear() {}
    }
}

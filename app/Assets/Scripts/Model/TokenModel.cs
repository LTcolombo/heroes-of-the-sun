using System;
using System.Linq;
using Settlement.Types;
using Solana.Unity.SDK;
using UnityEngine;
using Utils.Injection;
using Utils.Signal;

namespace Model
{
    [Singleton]
    public class TokenModel
    {
        public readonly Signal Updated = new();

        private const int DECIMALS = 9;

        private AccountLayout _data;

        public void Set(AccountLayout value)
        {
            _data = value;
            Updated.Dispatch();
        }


        public float Get()
        {
            return _data != null
                ? _data.amount / (float)Math.Pow(10, DECIMALS)
                : 0;
        }
    }
}
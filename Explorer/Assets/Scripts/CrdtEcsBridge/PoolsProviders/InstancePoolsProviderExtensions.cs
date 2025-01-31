﻿using Microsoft.ClearScript.JavaScript;
using System;

namespace CrdtEcsBridge.PoolsProviders
{
    public static class InstancePoolsProviderExtensions
    {
        public static void RenewCrdtRawDataPoolFromScriptArray(this IInstancePoolsProvider instancePoolsProvider,
            ITypedArray<byte> scriptArray,
            ref PoolableByteArray lastInput)
        {
            var intLength = (int)scriptArray.Length;

            // if the rented array can't keep the desired data, replace it
            if (lastInput.Array.Length < intLength)
            {
                // Release the old one
                lastInput.Dispose();

                // Rent a new one
                lastInput = instancePoolsProvider.GetAPIRawDataPool(intLength);
            }
            // Otherwise set the desired length to the existing array so it provides a correct span
            else
                lastInput.SetLength(intLength);

            // V8ScriptItem does not support zero length
            if (scriptArray.Length > 0)

                // otherwise use the existing one
                scriptArray.Read(0, scriptArray.Length, lastInput.Array, 0);
        }

        /// <summary>
        /// Releases the old array if it can't fit the new size anymore, and rents a new one.
        /// Copies data from the old array to the beginning of the new one
        /// </summary>
        public static PoolableByteArray Expand(this IInstancePoolsProvider instancePoolsProvider, PoolableByteArray lastInput, int newSize)
        {
            if (lastInput.Array.Length >= newSize)
            {
                lastInput.SetLength(newSize);
                return lastInput;
            }

            var newArray = instancePoolsProvider.GetAPIRawDataPool(newSize);

            lastInput.Array.CopyTo(newArray.Array, 0);
            lastInput.Dispose();

            return newArray;
        }

        public static void ReleaseAndDispose(this ref PoolableByteArray lastInput)
        {
            lastInput.Dispose();
            lastInput = PoolableByteArray.EMPTY;
        }
    }
}

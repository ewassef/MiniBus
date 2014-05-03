// Copyright (c) 2013 All Rights Reserved
// File        : IResponse
// Company     : ShortBus
// Author      : Jonathan Bost
// Create Date : 8/23/2013 9:19:42 PM
// Summary     : A interface that impliments appropriate responses

using System;

namespace ShortBus.Hostable.Shared.Interface
{
    /// <summary>
    /// This is the primary action that will be used to publish info to whoever cares on the bus
    /// </summary>
    /// <typeparam name="TIn">Any kind of message you with to publish</typeparam>
    public interface IFireAndForgetRequest<TIn>
    {
        Action<TIn> FireAndForgetRequest { get; set; }
    }
}
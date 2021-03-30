using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Models.Commands
{
    public class EmoteCommandQueue : ConcurrentQueue<EmoteCommand>
    {

    }
}

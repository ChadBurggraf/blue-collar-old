//-----------------------------------------------------------------------
// <copyright file="Limit.cs" company="Tasty Codes">
//     Modifications copyright (c) 2011 Chad Burggraf.
//     Original from Lokad Shared Libraries (http://lokad.com/).
//     
//     Copyright (c) 2008-2010, Lokad
//     All rights reserved.
//     
//     Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//     
//         * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//         * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//         * Neither the name of the Lokad nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//     
//     THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Threading;
    
    /// <summary>
    /// Provides helpers for limiting function invocations with a timeout.
    /// </summary>
    public sealed class Limit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Limit"/> class.
        /// </summary>
        /// <param name="duration">The duration to limit function invocations to.</param>
        public Limit(TimeSpan duration)
        {
            this.Duration = duration;
        }

        /// <summary>
        /// Gets the duration to limit function invocations to.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// Invokes the given function using this instance's <see cref="Timeout"/>.
        /// </summary>
        /// <param name="function">The function to invoke.</param>
        public void Invoke(Action function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function", "function cannot be null.");
            }

            object sync = new object();
            bool complete = false;

            WaitCallback callback =
                target =>
                {
                    Thread thread = target as Thread;

                    lock (sync)
                    {
                        if (!complete)
                        {
                            Monitor.Wait(sync, this.Duration);
                        }
                    }

                    if (!complete)
                    {
                        thread.Abort();
                    }
                };

            try
            {
                ThreadPool.QueueUserWorkItem(callback, Thread.CurrentThread);
                function();
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                throw new TimeoutException();
            }
            finally
            {
                lock (sync)
                {
                    complete = true;
                    Monitor.Pulse(sync);
                }
            }
        }

        /// <summary>
        /// Invokes the given function, limiting execution to the given timeout in milliseconds.
        /// </summary>
        /// <param name="function">The function to invoke.</param>
        /// <param name="duration">The duration, in milliseconds, to allow for execution.</param>
        public static void Invoke(Action function, int duration)
        {
            if (duration < 0)
            {
                throw new ArgumentOutOfRangeException("duration", "duration must be greater than or equal to 0.");
            }

            Invoke(function, new TimeSpan(0, 0, 0, 0, duration));
        }

        /// <summary>
        /// Invokes the given function, limiting execution to the given timeout.
        /// </summary>
        /// <param name="function">The function to invoke.</param>
        /// <param name="duration">The duration to allow for execution.</param>
        public static void Invoke(Action function, TimeSpan duration)
        {
            new Limit(duration).Invoke(function);
        }
    }
}

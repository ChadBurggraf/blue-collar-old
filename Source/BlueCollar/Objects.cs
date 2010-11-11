//-----------------------------------------------------------------------
// <copyright file="Objects.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides extensions and helpers to <see cref="System.Object"/>.
    /// </summary>
    public static class Objects
    {
        /// <summary>
        /// Copies any same-named property values from the source object to the destination object.
        /// Each destination property must be of a type that is assignable from the type
        /// of the corresponding source property.
        /// </summary>
        /// <param name="source">The source object to copy properties from.</param>
        /// <param name="destination">The destination object to copy properties to.</param>
        public static void CopyProperties(this object source, object destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source", "source must have a value.");
            }

            if (destination == null)
            {
                throw new ArgumentNullException("destination", "destination must have a value.");
            }

            var props = from s in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        join d in destination.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public) on s.Name equals d.Name
                        select new
                        {
                            SourceProp = s,
                            DestProp = d
                        };

            foreach (var prop in props)
            {
                if (prop.DestProp.CanWrite && prop.SourceProp.CanRead)
                {
                    object value = prop.SourceProp.GetValue(source, null);

                    if (prop.DestProp.PropertyType.IsAssignableFrom(prop.SourceProp.PropertyType))
                    {
                        prop.DestProp.SetValue(destination, value, null);
                    }
                }
            }
        }

        /// <summary>
        /// Safely raises an event on an object by first checking if the handler is null.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="handler">The event delegate.</param>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate here.")]
        public static void RaiseEvent(this object sender, EventHandler handler, EventArgs e)
        {
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// Safely raises an event on an object by first checking if the handler is null.
        /// </summary>
        /// <typeparam name="T">The type of the event arguments for the generic event being raised.</typeparam>
        /// <param name="sender">The event sender.</param>
        /// <param name="handler">The event delegate.</param>
        /// <param name="e">The event arguments.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Not appropriate here.")]
        public static void RaiseEvent<T>(this object sender, EventHandler<T> handler, T e) where T : EventArgs
        {
            if (handler != null)
            {
                handler(sender, e);
            }
        }
    }
}

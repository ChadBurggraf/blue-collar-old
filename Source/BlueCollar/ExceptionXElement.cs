//-----------------------------------------------------------------------
// <copyright file="ExceptionXElement.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
//     This was inspired from somewhere, but for the life of me I can't
//     remember where. Credit goes here when I figure it out.
// </copyright>
//-----------------------------------------------------------------------

namespace BlueCollar
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Creates an XML-Linq XElement from an Exception instance.
    /// </summary>
    public class ExceptionXElement : XElement
    {
        /// <summary>
        /// Initializes a new instance of the ExceptionXElement class.
        /// </summary>
        /// <param name="exception">An Exception to create an XElement from.</param>
        public ExceptionXElement(Exception exception)
            : base(SerializeToXElement(exception)) 
        { 
        }

        /// <summary>
        /// Serializes the given Exception to an XElement object.
        /// </summary>
        /// <param name="exception">The Exception to serialize.</param>
        /// <returns>The serialized Exception as an XElement.</returns>
        public static XElement SerializeToXElement(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            XElement root = new XElement(exception.GetType().ToString());
            root.Add(new XElement("Message", exception.Message ?? String.Empty));

            if (!String.IsNullOrEmpty(exception.StackTrace))
            {
                var stack = from f in exception.StackTrace.Split('\n')
                            let pf = f.Substring(6).Trim()
                            select new XElement("Frame", pf);

                root.Add(new XElement("StackTrace", stack));
            }
            else
            {
                root.Add(new XElement("StackTrace", String.Empty));
            }

            if (exception.Data.Count > 0)
            {
                var data = from e in exception.Data.Cast<DictionaryEntry>()
                           let k = e.Key.ToString()
                           let v = (e.Value ?? String.Empty).ToString()
                           select new XElement(k, v);

                root.Add(new XElement("Data", data));
            }
            else
            {
                root.Add(new XElement("Data", String.Empty));
            }

            if (exception.InnerException != null)
            {
                root.Add(new ExceptionXElement(exception.InnerException));
            }

            return root;
        }
    }
}
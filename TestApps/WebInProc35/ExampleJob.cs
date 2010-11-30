namespace WebInProc35
{
    using System;
    using System.Runtime.Serialization;
    using BlueCollar;

    [DataContract(Namespace = Job.XmlNamespace)]
    public class ExampleJob : Job
    {
        public override string Name
        {
            get { return "Example Job"; }
        }

        public override void Execute()
        {
        }
    }
}
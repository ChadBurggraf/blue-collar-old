

namespace Web40
{
    using System;
    using System.Runtime.Serialization;
    using BlueCollar;

    [DataContract(Namespace = Job.XmlNamespace)]
    public class ExampleScheduledJob : ScheduledJob
    {
        public override string Name
        {
            get { return "Example Scheduled Job"; }
        }

        public override void Execute()
        {
            
        }
    }
}
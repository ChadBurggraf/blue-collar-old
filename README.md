# Blue Collar
#### Background jobs + schedules for .NET

Blue Collar is a database-backed background/delayed job system for .NET applications. The system is very easy to install and use, and should be able to handle most situations you throw at it.

## Installing

Download `BlueCollar.msi` from the [downloads](http://github.com/ChadBurggraf/blue-collar/downloads) page. Double-click that bad boy. Bask.

You'll have to register your application with the service once you're ready to run some jobs, but first you'll need to actually write some jobs to run.

## Basic Usage

Add a reference for the appropriate framework version of the `BlueCollar.dll` assembly to your project. The assemblies should have been added to the GAC and the Visual Studio Add Reference dialog during installation.

You'll also need to add references to the appropriate assemblies for your database of choice (Blue Collar currently ships with storage implementations for SQL Server, PostgreSQL and SQLite).

Update your configuration file to tell the system about your storage layer, like so (example using SQL Server):

    <configuration>
	  <configSections>
	    <section name="blueCollar" type="BlueCollar.Configuration.BlueCollarSection, BlueCollar"/>
	  </configSections>
	  <appSettings>
	    <!-- You could have this  under connectionStrings instead if you like. -->
	    <add key="SqlServerConnectionString" value="YOUR_CONNECTION_STRING"/>
	  </appSettings>
	  <blueCollar>
	    <store type="BlueCollar.SqlServerJobStore, BlueCollar">
	      <metadata>
	        <add key="ConnectionStringName" value="SqlServerConnectionString"/>
	      </metadata>
	    </store>
	  </blueCollar>
	</configuration>

Now write a job to do some work. All jobs should store any state necessary for execution in instance properties or fields decorated with `DataMember` attributes. The job class itself should be decorated with a `DataContract` attribute, as shown in the example:

    namespace Yum
	{
	    using System;
	    using System.Net;
	    using System.Runtime.Serialization;
	    using BlueCollar;

	    [DataContract(Namespace = Job.XmlNamespace)]
	    public class SendEmailJob : Job
	    {
	        [DataMember]
	        public string EmailAddress { get; set; }

	        [DataMember]
	        public string Subject { get; set; }

	        [DataMember]
	        public string Body { get; set; }

	        public override string Name
	        {
	            get { return "Send Email"; }
	        }

	        public override void Execute()
	        {
	            using (MailMessage message = new MailMessage())
	            {
	                message.To.Add(this.EmailAddress);
	                message.Subject = this.Subject;
	                message.Body = this.Body;

	                new SmtpClient().Send(message);
	            }
	        }
	    }
	}

To execute the above job and actually send an email:

    new SendEmailJob()
	{
	    EmailAddress = "chad.burggraf@gmail.com",
	    Subject = "Hello, World!",
	    Body = "I carried that rock, boss. You got anything else for me?"
	}.Enqueue();
	
Sweet, now you have a job and your application is ready to go. Just register your application with the Blue Collar service by editing `collar_service.exe.config`, which should be under `C:\Program Files\Blue Collar` or similar. For web applications, setting the `name` and `directory` values will be enough; the `bin` directory and `Web.config` file will be inferred. If you do not set the `frameworkVersion` value, it will default to ThreeFive (the options are ThreeFive for 3.5, and FourZero for 4.0).

## Building From Source

There are two prerequisites to building the complete package from source on your machine (besides .NET Framework versions 3.5 and 4.0):

  1. [MSBuild Community Tasks](http://http://msbuildtasks.tigris.org/)
  2. [WiX v3.5](http://wix.sourceforge.net/downloadv35.html)

With both of those installed, you should be able to execute `build-and-package.bat` from the console. The output will end up in `Build/`, and will include the binaries plus the `BlueCollar.msi` installation package.

## Documentation

I'm working on documentation as we speak. I'll add links here when it is at least usable.

## License

Licensed under the [MIT](http://www.opensource.org/licenses/mit-license.html) license. See LICENSE.txt.

Copyright (c) 2010 Chad Burggraf.







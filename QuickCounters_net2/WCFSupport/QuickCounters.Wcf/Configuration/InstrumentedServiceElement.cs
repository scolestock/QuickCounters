
//
// InstrumentedServiceElement.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Text;

namespace QuickCounters.Wcf.Configuration
{

   /// <summary>
   /// Configuration Element for the InstrumentedServiceBehavior 
   /// for Windows Communication Foundation
   /// </summary>
   public class InstrumentedServiceElement : BehaviorExtensionElement
   {
      private const string SERVICE_PS = "service";
      private const string ENABLEHTTPGET_PS = "enableHttpGet";
      private const string ADDRESS_PS = "address";
      private const string APPNAME_PS = "applicationName";
      private ConfigurationPropertyCollection _properties;

      /// <summary>
      /// Service prefix to use instead of the name
      /// of the service class
      /// </summary>
      [ConfigurationProperty(SERVICE_PS)]
      public string Service
      {
         get { return (string)base[SERVICE_PS]; }
         set { base[SERVICE_PS] = value; }
      }

      /// <summary>
      /// Enables or disables HTTP Get of the 
      /// instrumentation metadata
      /// </summary>
      [ConfigurationProperty(ENABLEHTTPGET_PS)]
      public bool EnableHttpGet
      {
         get { return (bool)base[ENABLEHTTPGET_PS]; }
         set { base[ENABLEHTTPGET_PS] = value; }
      }

      /// <summary>
      /// Address (relative or absolute uri) on which
      /// to host the metadata
      /// </summary>
      [ConfigurationProperty(ADDRESS_PS)]
      public string Address
      {
         get { return (string)base[ADDRESS_PS]; }
         set { base[ADDRESS_PS] = value; }
      }

      /// <summary>
      /// Name of the Application to generate
      /// </summary>
      [ConfigurationProperty(APPNAME_PS)]
      public string ApplicationName
      {
         get { return (string)base[APPNAME_PS]; }
         set { base[APPNAME_PS] = value; }
      }

      /// <summary>
      /// Return the type of the behavior we configure
      /// </summary>
      public override Type BehaviorType
      {
         get { return typeof(InstrumentedServiceAttribute); }
      }


      /// <summary>
      /// Return a collection of all our properties
      /// </summary>
      protected override ConfigurationPropertyCollection Properties
      {
         get
         {
            if ( _properties == null )
            {
               _properties = new ConfigurationPropertyCollection();
               _properties.Add(new ConfigurationProperty(
                  SERVICE_PS, typeof(string), "",
                  ConfigurationPropertyOptions.None
                  ));
               _properties.Add(new ConfigurationProperty(
                  ADDRESS_PS, typeof(string), "",
                  ConfigurationPropertyOptions.None
                  ));
               _properties.Add(new ConfigurationProperty(
                  ENABLEHTTPGET_PS, typeof(bool), false,
                  ConfigurationPropertyOptions.None
                  ));
               _properties.Add(new ConfigurationProperty(
                  APPNAME_PS, typeof(string), "",
                  ConfigurationPropertyOptions.None
                  ));
            }
            return _properties;
         }
      }

      /// <summary>
      /// Create an instance of the behavior
      /// we represent
      /// </summary>
      /// <returns>The InstrumentedServiceBehavior instance</returns>
      protected override object CreateBehavior()
      {
         InstrumentedServiceAttribute ins =
            new InstrumentedServiceAttribute();
         ins.Service = Service;
         ins.EnableHttpGet = EnableHttpGet;
         ins.Address = Address;
         ins.ApplicationName = ApplicationName;

         return ins;
      }

      /// <summary>
      /// Copy the information of another element into
      /// ourselves
      /// </summary>
      /// <param name="from">The element from which to copy</param>
      public override void CopyFrom(ServiceModelExtensionElement from)
      {
         base.CopyFrom(from);
         InstrumentedServiceElement element = 
            (InstrumentedServiceElement)from;
         Service = element.Service;
         EnableHttpGet = element.EnableHttpGet;
         Address = element.Address;
         ApplicationName = element.ApplicationName;
      }

   } // class InstrumentedServiceElement

} // namespace QuickCounters.Wcf.Configuration


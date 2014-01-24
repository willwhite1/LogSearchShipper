
## logstash-forwarder.NET

A Windows optimised version of the elasticsearch/logstash-forwarder (in .NET).

Currenty this is just a wrapper aroung the Go elasticsearch/logstash-forwarder that adds .NET style config 
and the ability to run as a Windows Service.

_NB:  This (currently) only supports Windows.  Run the native elasticsearch/logstash-forwarder on other platforms_

### Config

Edit `LogstashForwarder.Service.exe.config` and configure the `logstashForwarderGroup` to point at the files you want shipped.

```
  <logstashForwarderGroup>
    <logstashForwarder servers="logstash.yourdomain.co.uk:5043" ssl_ca="C:\Logs\yourdomain.cer" timeout="42">
      <watch files="C:\Logs\Service1.log" type="json">
        <field key="@environment" value="DEV" />
      </watch>
      <watch files="C:\Logs\Service2.log" type="log4net">
        <field key="@environment" value="DEV" />
      </watch>
    </logstashForwarder>
  </logstashForwarderGroup>
 ```

#### Running from the Command line 

```
LogstashForwarder.Service.exe
```

#### Running as a Windows Service

```
LogstashForwarder.Service.exe install --sudo
```

#### Help

See

```
LogstashForwarder.Service.exe help
```

### License

Copyright 2013 City Index

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
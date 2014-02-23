## logsearch-shipper.NET [![Build Status](http://jenkins.cityindex.logsearch.io/buildStatus/icon?job=logsearch-logstash-forwarder-NET)](http://jenkins.cityindex.logsearch.io/job/logsearch-logstash-forwarder-NET/)

A Windows optimised shipper for getting logs into Logsearch.

Currenty this is just a wrapper aroung the Go elasticsearch/logstash-forwarder that adds .NET style config 
and the ability to run as a Windows Service.

_NB:  This (currently) only supports Windows.  Run the native elasticsearch/logstash-forwarder on other platforms_

### Config

Edit `LogsearchShipper.Service.exe.config` and configure the `LogsearchShipperGroup` to point at the files you want shipped.

```xml
  <LogsearchShipperGroup>
    <LogsearchShipper servers="ingestor.example.com:5043" ssl_ca="C:\Logs\mycert.crt" timeout="23">
      <fileWatchers>
       <watch files="myfile.log" type="myfile_type">
          <field key="field1" value="field1 value" />
          <field key="field2" value="field2 value" />
        </watch>
        <watch files="C:\Logs\myfile.log" type="type/subtype">
          <field key="key/subkey" value="value/subvalue" />
        </watch>
      </fileWatchers>
    </LogsearchShipper>
  </LogsearchShipperGroup>
 ```

#### Running from the Command line 

```
LogsearchShipper.Service.exe
```

#### Running as a Windows Service

```
LogsearchShipper.Service.exe install --sudo
```

#### Help

See

```
LogsearchShipper.Service.exe help
```

### License

Copyright 2013-2014 City Index

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

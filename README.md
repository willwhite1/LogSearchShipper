## LogSearchShipper

A Windows optimised shipper for getting logs into your Logsearch cluster

### Installing

* `SHIPPER_USERNAME`/`SHIPPER_PASSWORD` - A Windows domain account that has read only access to the log files you want to ship.  
Must also have premissions to register and start/stop other services (eg, be part of the Local machines Admin group)
* `D:\Apps\LogSearchShipper\current` - `LogSearchShipper.exe` + `.dll`s go here. `SHIPPER_USERNAME` must have read access to this folder
* `D:\Apps\LogSearchShipper\data` - Working folder for LogSearchShipper.  `SHIPPER_USERNAME` *must* have full write access to this folder
* `D:\Logs\LogSearchShipper` - Folder for LogSearchShipper to write its logs to.`SHIPPER_USERNAME` *must* have full write access to this folder
### Config

Rename `LogSearchShipper.exe.config.SAMPLE` to `LogsearchShipper.Service.exe.config`
* configure the `LogsearchShipperGroup` to point at the files you want shipped.
* configure `ingestor_*` to the logsearch cluster you want to ship to
* configure `shipper_service_username`/`shipper_service_password` to `SHIPPER_USERNAME`/`SHIPPER_PASSWORD`
* configure `data_folder` to `D:\Apps\LogSearchShipper\data`
* configure various `Log4Net` `FileAppender` to `D:\Logs\LogSearchShipper`

#### Running as a Windows Service

```
LogSearchShipper.exe install --sudo -username:`SHIPPER_USERNAME` -password:`SHIPPER_PASSWORD`
LogSearchShipper.exe start
```

#### Help

See

```
LogSearchShipper.exe help
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

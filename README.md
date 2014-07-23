## LogSearchShipper

A Windows optimised shipper for getting logs into your Logsearch cluster

### Installing

* Download latest builds from from http://ci-logsearch-shipper.s3-website-eu-west-1.amazonaws.com/
* Unzip into a new folder

### Config

Rename `LogSearchShipper.exe.config.SAMPLE` to `LogsearchShipper.Service.exe.config` and configure the `LogsearchShipperGroup` to point at the files you want shipped.

#### Running from the Command line 

```
LogSearchShipper.exe
```

#### Running as a Windows Service

```
LogSearchShipper.exe install --sudo
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

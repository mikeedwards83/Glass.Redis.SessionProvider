# Glass Redis Session Provider for Sitecore

This is an Redis Session Provider that supports the session end event to allow support for XDB.

Thanks goes to [Nick Hills](https://github.com/boro2g/Sitecore-Redis-Session-Provider) who created the first redis provider 
and provided a lot of inspiration for this implementation.

This implementation features the following:

* Session keys are recorded in a separate table against expiry time and cleared by each server polling this table.
* When a session end occurs and is processed the session data is locked to avoid multiple servers processing the same data. 
* Locks on session end data expire, therefore if a server fails to process the session data then another server can try later.
* Session end locking mechanism based on the same mechanism used by the Microsoft Redis Session provider to lock a session to a user during a page request.
* Unit tests where possible - HTTPContext creates some difficulties.
* Differentiates between shared and private sessions. You can use the provider for both.

This implementation is still being tested. Any feedback is welcome.


## Licence

   Copyright 2016 Michael Edwards

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this project except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
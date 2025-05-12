# SimpeTelnetClient
The simplest working prototype of a telnet client for connecting and operating with a juniper switch. In this case, it is implemented:
1. Connect and log in (login information in a json file, password encrypted)
2. checking active pppoe sessions of subscribers by sending the "show subscribers | match {login}" command
3. PPPOE interface verification (HARD CODED)
4. Data output to the UI
5. Working with the Mac Vendors api

The project is executed using the simplest implementation of the MVVM pattern, conforms to the principles of SOLID, with detailed logging in a txt file
![Image alt](https://github.com/alcher96/SimpeTelnetClient/blob/main/image.png)

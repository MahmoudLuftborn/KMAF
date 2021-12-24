/*********
  Rui Santos
  Complete project details at https://randomnerdtutorials.com  
*********/

// Load Wi-Fi library
#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <AsyncTCP.h>
#include <ESPAsyncWebServer.h>
#include <EEPROM.h>
#include <TaskScheduler.h>

#define EEPROM_SIZE 4096

// Replace with your network credentials
const char* ssid     = "YYY";
const char* password = "123456789";
const char* mqtt_server = "51.15.236.147";
const char* SSID_INPUT = "ssid_input";
const char* PASSWORD_INPUT = "password_input";
const char* IP_INPUT = "ip_input";
String ssidConfigurationValue = "";
String passwordConfigurationValue = "";
String ipConfigurationValue = "";
bool enableReading = false;

void t1Callback();
Task t1(3000, TASK_FOREVER, &t1Callback);
Scheduler runner;

// Set web server port number to 80
AsyncWebServer  server(80);

// HTML web page to handle 32 input fields (input1, input2)
const char Startup_Configuration[] PROGMEM = R"rawliteral(
<!DOCTYPE HTML><html><head>
  <title>ESP32 Configuration</title>
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <style>  
    .btn {
      width: 60px;
      height: 30px;
      margin-top: 10px;
    }

    .btn.primary {
      background-color: #333333;
      color: white;
      broder-radius: 7px;
      border: none
    }

    .p-input {
      font-size: small;
    }

    .header {
      margin-left: auto;
      margin-right: auto;
      color: grey;
    }

    .container{
      display: flex;
      flex-direction: column;
      margin-left: auto;
      margin-right: auto;
    }

    .config-container{
      margin-left: auto;
      margin-right: auto;
      margin-top: 5px;
      margin-botton: 5px;
      padding: 15px;
      display: flex;
      flex-direction: column;
      background-color: #F8F8FF;
      box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;
      justify-content: center
      align-items: center;
      align-content: center;
    }

    form {
      display: block;
    }
    
  </style>
  </head><body>
  <div class= "container">
  <h1 class="header">ESP32 Configuration</h1>
  <div class="config-container">
   <form action="/get">
   <p>Please enter the ssid and password for the network you want to configure.</p>
   <p class="p-input">ssid: </p>
    <input type="text" name="ssid_input">
    <br>
    <p class="p-input">password: </p>
    <input type="password" name="password_input">
    <br>
   <p>Enter the Message Broker IP Address.</p>
   <p class="p-input"> IP Address: </P>
   <input type="text" name="ip_input">
   <br>
    <input class="btn primary" type="submit" value="Submit">
  </form>
  </div>
  </div>
</body></html>)rawliteral";

WiFiClient espClient;
PubSubClient client(espClient);
long lastMsg = 0;
char msg[50];
int value = 0;

// Variable to store the HTTP request
String header;

// Auxiliar variables to store the current output state
String output36State = "off";

// Assign output variables to GPIO pins
const int output36 = 36;
bool configured = false;


void setup() {
  EEPROM.begin(EEPROM_SIZE);

  runner.init();
  runner.addTask(t1);
  t1.enable();
  
  Serial.begin(115200);
  // Initialize the output variables as outputs
  pinMode(output36, OUTPUT);
  // Set outputs to LOW
  digitalWrite(output36, LOW);

  // Connect to Wi-Fi network with SSID and password
  Serial.print("Setting AP (Access Point)…");
  // Remove the password parameter, if you want the AP (Access Point) to be open
  WiFi.softAP(ssid, password);

  IPAddress IP = WiFi.softAPIP();
  Serial.print("AP IP address: ");
  Serial.println(IP);
  setupCofigurationPage();
  server.onNotFound(notFound);
  server.begin();
}

void loop(){
  setValues();

  if(configured){
    initWiFi();
    initMQTT();
  runner.execute();
  }
}

void initWiFi() {
  if(WiFi.status() != WL_CONNECTED){
    WiFi.mode(WIFI_STA);
    char ssidBuf[50];
    char pswdBuf[50];
    ssidConfigurationValue.toCharArray(ssidBuf, 50);
    passwordConfigurationValue.toCharArray(pswdBuf, 50);
    
    //String ssid = ssidConfigurationValue.c_str();
 //String pswd = passwordConfigurationValue.c_str();

  //String ssid = readStringFromEEPROM(0);
  //String pwd = readStringFromEEPROM(300);
  
  //Serial.println(ssid);
  //Serial.println(pwd);

    WiFi.begin("IOT-MiFi", "Pkma@1993");
    Serial.print("Connecting to WiFi .. Pkma@1993");
      while (WiFi.status() != WL_CONNECTED) {
        Serial.print('.');
        delay(1000);
      }
      Serial.println(WiFi.localIP());
  }
}

void setValues(){

  String ipConfigurationValue = readStringFromEEPROM(600);
  String isConfigured = readStringFromEEPROM(900);

  //Serial.println(ssidConfigurationValue);
  //Serial.println(passwordConfigurationValue);
  //Serial.println(ipConfigurationValue);
  //Serial.println(isConfigured);

  if(isConfigured == "1"){
    configured = true;
  }else{
    configured = false;
  }
  
  //Serial.println(configured);
}

void initMQTT(){
  client.setServer(mqtt_server, 1883);
    client.setCallback(callback);
  while (!client.connected()) {


    Serial.print("Attempting MQTT connection...");
    // Attempt to connect
    if (client.connect("espClient")) {
      Serial.println("connected");
      // Subscribe
    String temp = ssidConfigurationValue + ';' + passwordConfigurationValue + ';' + ipConfigurationValue + ';' + enableReading;
    String sendTopic = "esp32/data";

    
    client.publish(sendTopic.c_str(),temp.c_str());

      client.subscribe("esp32/test");
      Serial.print(client.state());
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5 seconds");
      // Wait 5 seconds before retrying
      delay(5000);
    }
  }
  client.loop();

}

String getValue(String data, char separator, int index)
{
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length()-1;

  for(int i=0; i<=maxIndex && found<=index; i++){
    if(data.charAt(i)==separator || i==maxIndex){
        found++;
        strIndex[0] = strIndex[1]+1;
        strIndex[1] = (i == maxIndex) ? i+1 : i;
    }
  }

  return found>index ? data.substring(strIndex[0], strIndex[1]) : "";
}

void callback(char* topic, byte* message, unsigned int length) {
  Serial.print("Message arrived on topic: ");
  Serial.print(topic);
  Serial.print(". Message: ");
  String messageTemp;
  
  for (int i = 0; i < length; i++) {
    Serial.print((char)message[i]);
    messageTemp += (char)message[i];
  }
  Serial.println();
  Serial.print(messageTemp);

  String ssid = getValue(messageTemp,';',0);
  String pwd = getValue(messageTemp,';',1);
  String ip = getValue(messageTemp,';',2);
  String enableR = getValue(messageTemp,';',3);

  Serial.println(ssid);
  Serial.println(pwd);
  Serial.println(ip);
  Serial.println(enableR);
}

void notFound(AsyncWebServerRequest *request) {
  request->send(404, "text/plain", "Not found");
}

void setupCofigurationPage(){
   // Send web page with input fields to client
  server.on("/", HTTP_GET, [](AsyncWebServerRequest *request){
    request->send_P(200, "text/html", Startup_Configuration);
  });

  // Send a GET request to <ESP_IP>/get?input1=<inputMessage>
  server.on("/get", HTTP_GET, [] (AsyncWebServerRequest *request) {
    String ssidInputMessage = "N/A";
    String ssidInputParam = "N/A";
    String passwordInputMessage = "N/A";
    String passwordInputParam = "N/A";
    String ipInputMessage = "N/A";
    String ipInputParam = "N/A";
    configured = true;
    
    // GET ssid value on <ESP_IP>/get?input1=<inputMessage>
    if (request->hasParam(SSID_INPUT)) {
      ssidConfigurationValue = request->getParam(SSID_INPUT)->value();
      ssidInputParam = SSID_INPUT;
    }
    // GET password value on <ESP_IP>/get?input2=<inputMessage>
    if (request->hasParam(PASSWORD_INPUT)) {
      passwordConfigurationValue = request->getParam(PASSWORD_INPUT)->value();
      passwordInputParam = PASSWORD_INPUT;
    }

    // GET ip address value on <ESP_IP>/get?input2=<inputMessage>
    if (request->hasParam(IP_INPUT)) {
      ipConfigurationValue = request->getParam(IP_INPUT)->value();
      ipInputParam = PASSWORD_INPUT;
    }
    
    Serial.println(ssidConfigurationValue);
    Serial.println(passwordConfigurationValue);
    Serial.println(ipConfigurationValue);

    writeStringToEEPROM(0, ssidConfigurationValue);
    writeStringToEEPROM(300, passwordConfigurationValue);
    writeStringToEEPROM(600, ipConfigurationValue);
    writeStringToEEPROM(900, String(configured));

    request->send(200, "text/html", "HTTP GET request sent to your ESP on input field (" 
                                     + ssidInputParam + ") with value: " + ssidInputMessage +
                                     " and input field (" + passwordInputParam + ") with value: " + passwordInputMessage +
                                     " and input field (" + ipInputParam + ") with value: " + ipInputMessage +
                                     "<br><a href=\"/\">Return to Home Page</a>");
  });
}

void writeStringToEEPROM(int addrOffset, const String &strToWrite)
{
  byte len = strToWrite.length();
  EEPROM.write(addrOffset, len);

  for (int i = 0; i < len; i++)
  {
    EEPROM.write(addrOffset + 1 + i, strToWrite[i]);
  }
  EEPROM.commit();
}

String readStringFromEEPROM(int addrOffset)
{
  int newStrLen = EEPROM.read(addrOffset);
  char data[newStrLen + 1];

  for (int i = 0; i < newStrLen; i++)
  {
    data[i] = EEPROM.read(addrOffset + 1 + i);
  }
  data[newStrLen] = '\0'; // !!! NOTE !!! Remove the space between the slash "/" and "0" (I've added a space because otherwise there is a display bug)

  return String(data);
}

void t1Callback() {
    Serial.print("sending readings ....");
  long rssi = WiFi.RSSI();
  String reading = String(rssi);

    String sendTopic = "esp32/reading";
    client.publish(sendTopic.c_str(),reading.c_str());
}
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


// Replace with your network credentials
const char* ssid     = "YYY";
const char* password = "123456789";
const char* mqtt_server = "51.15.236.147";
const char* SSID_INPUT = "ssid_input";
const char* PASSWORD_INPUT = "password_input";
const char* IP_INPUT = "ip_input";
String ssidConfigurationValue = "";
String passwordConfigurationValue = "";

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
  if(configured){
    initWiFi();
    initMQTT();
  }
}

void initWiFi() {
  if(WiFi.status() != WL_CONNECTED){
    WiFi.mode(WIFI_STA);
    WiFi.begin("IOT-MiFi", "Pkma@1993");
    Serial.print("Connecting to WiFi .. Pkma@1993");
      while (WiFi.status() != WL_CONNECTED) {
        Serial.print('.');
        delay(1000);
      }
      Serial.println(WiFi.localIP());
  }
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
      ssidInputMessage = request->getParam(SSID_INPUT)->value();
      ssidInputParam = SSID_INPUT;
    }
    // GET password value on <ESP_IP>/get?input2=<inputMessage>
    if (request->hasParam(PASSWORD_INPUT)) {
      passwordInputMessage = request->getParam(PASSWORD_INPUT)->value();
      passwordInputParam = PASSWORD_INPUT;
    }

    // GET ip address value on <ESP_IP>/get?input2=<inputMessage>
    if (request->hasParam(IP_INPUT)) {
      ipInputMessage = request->getParam(IP_INPUT)->value();
      ipInputParam = PASSWORD_INPUT;
    }
    
    Serial.println(ssidInputMessage);
    Serial.println(passwordInputMessage);
    Serial.println(ipInputMessage);
    request->send(200, "text/html", "HTTP GET request sent to your ESP on input field (" 
                                     + ssidInputParam + ") with value: " + ssidInputMessage +
                                     " and input field (" + passwordInputParam + ") with value: " + passwordInputMessage +
                                     " and input field (" + ipInputParam + ") with value: " + ipInputMessage +
                                     "<br><a href=\"/\">Return to Home Page</a>");
  });
}

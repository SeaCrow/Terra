// Terra module, unlock SOFTSPI in RF24_config.h before uploading !
//#include <SPI.h>

#include "RF24.h"        // nRF radio library declarations
RF24 radio(7,8);
byte addresses[][6] = {"1Node","2Node"};

#include "DHT.h"         // DHT (temperature and humidity sensor)
DHT dht(6, DHT22, 4);    // library and declarations (pin, type, speed)

#include <avr/wdt.h>     // Watchdog timer	
#include "EEPROM.h"      // Library for EEPROM memory

#include <Wire.h>        // RTC declarations
#include <DS3231.h>
DS3231 clock;
RTCDateTime dt;

#define ledPin  5             // Built-in blue LED
#define redPin  9             // Red LED,   connected to digital pin 9
#define grnPin  10            // Green LED, connected to digital pin 10
#define bluPin  11            // Blue LED,  connected to digital pin 11
#define rl1Pin 2              // Relay 1,   connected to digital pin 2, Water pump is connected to this realay
#define rl2Pin 3              // Relay 2,   connected to digital pin 3, Heat lamp (230V) is connected to this realay 
#define rl3Pin 4              // Relay 3,   connected to digital pin 4, Fans are connected to this realay
//#define fan1Pin A2            // Fan 1,   connected to analog pin 2
//s#define fan2Pin A3            // Fan 2,   connected to analog pin 3
#define cHour sensors[3][2]   // Currrent Hour
#define cMinute sensors[3][3] // Current  Minute
#define CDhh settings[6][2]   // Hour and Minute of Start time of Day phase
#define CDmm settings[6][3]
#define CNhh settings[7][2]   // Hour and Minute of Start time of Night phase
#define CNmm settings[7][3]
#define CShh settings[8][2]   // Hour and Minute of Start time of Dawn phase
#define CSmm settings[8][3]
#define CZhh settings[9][2]   // Hour and Minute of Start time of Twilight phase
#define CZmm settings[9][3]
#define COhh settings[10][2]  // Hour and Minute of Start time of Dark phase 
#define COmm settings[10][3]

int redPWM = 0;
int grnPWM = 0;
int bluPWM = 0;
int fanControlID;
float holdTemp, holdAHum, holdGHum, tmpFloat;

#define blinkDelay  300
#define sendDelay  1000

String code = "";
byte data[5];  // Buffer for sending and reciving from nRF radio
byte settings[13][5]; // Array holding all settings
/*
settings[0][]  LD - Led Daylight                   [3]
settings[1][]  LN - Led Night/Moonlight            [3]
settings[2][]  LS - Led Dawn/Sunrise               [3]
settings[3][]  LZ - Led Twilight/Sunset            [3]
settings[4][]  TD - Daytime Temperature            [2]
settings[5][]  TN - Nighttime Temperature          [2]
settings[6][]  CD - Start time of Day phase        [2]
settings[7][]  CN - Start time of Night phase      [2]
settings[8][]  CS - Start time of Dawn phase       [2]
settings[9][]  CZ - Start time of Twilight phase   [2]
settings[10][] CO - Start time of Dark phase       [2]
settings[11][] WP - Air Humidity                   [2]
settings[12][] WG - Ground Humidity                [2]
*/
byte sensors[4][4]; // Array holding most recent sensor read out and current clock
/*
sensors[0][] TT - Air Temperature [2]
sensors[1][] AA - Air Humidity    [2]
sensors[2][] GG - Ground Humidity [2]
sensors[3][] CC - Current Clock   [2]
*/

unsigned long SensorWaitTime;
unsigned long WaitTime;
unsigned long WaterPulse;
void setup()
{
  wdt_disable();             // disables watchdog
  wdt_enable(WDTO_4S);       // enables watchdog with 8s delay
  
  pinMode(redPin, OUTPUT);   // sets the pins as output
  pinMode(grnPin, OUTPUT);   
  pinMode(bluPin, OUTPUT);
  pinMode(ledPin, OUTPUT);
  pinMode(rl1Pin, OUTPUT);
  pinMode(rl2Pin, OUTPUT);
  pinMode(rl3Pin, OUTPUT);
  //pinMode(fan1Pin, OUTPUT);
  //pinMode(fan2Pin, OUTPUT);
  
  digitalWrite(rl1Pin, HIGH); // relay is in standard (off) position when it recives HIGH signal
  digitalWrite(rl2Pin, HIGH);
  digitalWrite(rl3Pin, HIGH); 
  
  holdTemp = 25;
  holdAHum = 35;
  holdGHum = 30;
  
  radio.begin();
  radio.setRetries(15,15);
  //radio.setPALevel(RF24_PA_LOW);
  radio.openWritingPipe(addresses[0]);   //TERRA
  radio.openReadingPipe(1,addresses[1]);
  radio.startListening();
  
  dht.begin();
  
  clock.begin();
  dt = clock.getDateTime();

  DummySensor();    // Formating sensor array and filling it with starting data
  //DummySettings();  // Formatting settings array and filling it with starting data
  ReadEeprom();     // Loading previously saved settings to array.
  
  digitalWrite(ledPin, HIGH);
  delay(blinkDelay);
  digitalWrite(ledPin, LOW);
  
  SensorWaitTime = millis();
  WaitTime = millis();
  
  fanControlID = 0;
  RGBled(0,1,0);
  
  wdt_reset();    // resets watchdog countdown.
}

void loop()
{
  wdt_reset();
  
  if(radio.available())  // recived data from radio
  {
    radio.read(&data, sizeof(data));
    recivedData(); // parse recived data
  }
  
  if(millis() - SensorWaitTime > 5000) // waited long enough, check sensors
  {
    checkSensors();
  }
  
  if(millis() - WaitTime > 10000) // waited long enough, see if there is need to do anything
  {
    checkSettings();
  }
 
} // end loop
void checkSettings()
{
    int i = dayPhase(); // lets check current day phase
    if( i != -1){checkRGB(i);} // check if there is need to change rgb led depending of current day phase
    if( i == -1){checkRGB();}
    
    // ------------------------------------Temperature-Operations--Id-1-------------------------------
    if( i == 0 || i == 3) // its day temperature setting
    { 
      holdTemp = settings[4][2];
    }
    if( i == -1 || i == 1 || i == 2) // its night temperature setting
    {
      holdTemp = settings[5][2];
    }
    if( i != -1)
    {
     tmpFloat = sensors[0][2];  // loading current temperature into temporary float
     if(abs(tmpFloat - holdTemp) >= 2) // if temperature deviates from what was set 2 or more degrees, do something
     {
       if(tmpFloat < holdTemp){ digitalWrite(rl2Pin, LOW); } // its cold, turn on the heater
       if(tmpFloat > holdTemp) // too hot, vent the terrarium
       {
         fanControl(1,1);  // request start of fans
       }
     }
     else // temperature is fine, do nothing/ turn off heater/ fans
     {
       digitalWrite(rl2Pin, HIGH);  // shutdown heater
       fanControl(0,1); // call for fan shutdown
     }// ------------------------------------------------------------------------------------------------
    
     // ------------------------------------Air-Humidity-Operations--Id-2--------------------------------- 
      holdAHum = settings[11][2];
      tmpFloat = sensors[1][2];
     
     if(abs(tmpFloat - holdAHum) >= 5) // if air humidity deviates from what was set 5 or more percent, do something
     {
       if(tmpFloat < holdAHum) // too dry, spray the terraium
       {
         digitalWrite(rl1Pin, LOW);  // 2 sec long water spray
         delay(2000);                // using delay instead of milis to avoid incidents
         digitalWrite(rl1Pin, HIGH);
       }
       if(tmpFloat > holdAHum) // too damp, vent the terrarium
       {
         fanControl(1,2);  // request start of fans
       }
     }
     else // air humidity is fine, do nothing/ turn off fans
     {
       fanControl(0,2); // call for fan shutdown
     }// ------------------------------------------------------------------------------------------------
    
     // ------------------------------------Ground-Humidity-Operations--Id-3-----------------------------
     holdGHum = settings[12][2]; // loading float value of ground humidity setting storage
    
     tmpFloat = sensors[2][2];  // loading current ground humidity into temporary float
    
     if(abs(tmpFloat - holdGHum) >= 5) // if ground humidity deviates from what was set 5 or more percent, do something
     {
       if(tmpFloat < holdGHum) // soil too dry, need  to spray it
       {
        
         digitalWrite(rl1Pin, LOW);  // 2 sec long water spray
         delay(2000);
         digitalWrite(rl1Pin, HIGH);
        
       }
       if(tmpFloat > holdGHum) // soil too wet, need  to dry it. But how?
       {                       // there is currently no real way to get rid of excess water in the ground                             
                              // fans can only remove it from the air and they will be working thanks to air sensor
                              // when there will be need for it as water evaporates from the ground
       }                       // structure created for sake of standardization and for potential future system expansion (for example: heating mat)
     }
     else // ground humidity is fine, do nothing
     {
       // nothing to do here, water spray ends on its own and we have no means to handle damp ground
      
     }
    }else{
	digitalWrite(rl3Pin, HIGH);
        fanControlID = 0;
        digitalWrite(rl2Pin, HIGH);
    }

    // ------------------------------------------------------------------------------------------------
    WaitTime = millis(); // mark time of parameters check
}
void checkSensors()
{
    float temp = dht.readTemperature(); // read air parameters from the sensor
    float hum = dht.readHumidity();
    if (!isnan(temp) && !isnan(hum)) // if sensor worked
    {  
       int I = temp;                 // convert readout to 2 bit format and save it
       int F = (temp - I)*10;
       sensors[0][2] = byte((I + sensors[0][2])/2);
       sensors[0][3] = byte(F + sensors[0][3]);
       
       I = hum;
       F = (hum - I)*10;
       sensors[1][2] = byte((I + sensors[1][2])/2);
       sensors[1][3] = byte(F + sensors[1][3]);
    }
    if (isnan(temp) && isnan(hum)){digitalWrite(rl1Pin, HIGH);digitalWrite(rl2Pin, HIGH);digitalWrite(rl3Pin, HIGH);} // temperature sensor failed emergency shutdown of a heater, water pump and fans
    
    int wgRead = analogRead(A1); // read ground parameter
    handleWG(wgRead);            // convert it to percentage value and save it.
    
    dt = clock.getDateTime();        // grab curent datetime from RTC
    sensors[3][2] = byte(dt.hour);   // save it for later use in 2 bit format
    sensors[3][3] = byte(dt.minute);
    
    SensorWaitTime = millis();   // mark time of sensor readout 
}
void fanControl(int state, int id)
{
  if (fanControlID == id || fanControlID == 0) // proceed if call is made from authorized process or there is no vent session in progress
  {
    if(state == 1)
    {
      digitalWrite(rl3Pin, LOW);  // start both fans
      //digitalWrite(fan2Pin, HIGH);
      
      fanControlID = id;            // save id of the ventilation request
    }
    if(state == 0)
    {
      digitalWrite(rl3Pin, HIGH);  // shutdown both fans
      //digitalWrite(fan2Pin, LOW);
      
      fanControlID = 0;            // clear ID on finished vent session
    }
    
  }
}
void checkRGB(int i)
{
 if(settings[i][2] != redPWM || settings[i][3] != grnPWM || settings[i][4] != bluPWM)
 { RGBled(settings[i][2],settings[i][3],settings[i][4]); }
 
}
void checkRGB()
{
 if(redPWM != 0 || grnPWM != 0 || bluPWM != 0)
 { RGBled(0,0,0); }
 
}
void RGBled(int r, int g, int b){
  
  if (r < 0){r = 0;}
  if (r > 255){r = 255;}
  redPWM = r;
  
  if (g < 0){g = 0;}
  if (g > 255){g = 255;}
  grnPWM = g;

  if (b < 0){b = 0;}
  if (b > 255){b = 255;}
  bluPWM = b;  
  
  analogWrite(redPin, redPWM);
  analogWrite(grnPin, grnPWM);
  analogWrite(bluPin, bluPWM);
}
void DummySensor()
{
  sensors[0][0] = 'T';
  sensors[0][1] = 'T';
  sensors[0][2] = byte(20);
  sensors[0][3] = byte(0);
  
  sensors[1][0] = 'A';
  sensors[1][1] = 'A';
  sensors[1][2] = byte(30);
  sensors[1][3] = byte(0);
  
  sensors[2][0] = 'G';
  sensors[2][1] = 'G';
  sensors[2][2] = byte(30);
  sensors[2][3] = byte(0);
  
  sensors[3][0] = 'C';
  sensors[3][1] = 'C';
  sensors[3][2] = byte(24);
  sensors[3][3] = byte(12);
  
  
}
void handleWG (int sensor)
{
  if(sensor < 250){sensor = 250;} // very wet soil
  if(sensor > 1000){sensor = 1000;} // very dry soil
  
  float WG = (sensor - 1000)*(-1); // 0 - 750 : 0 = dry, 750 = wet.
  WG = (WG * 100) / 750; //WG holds % value now
  
  int I = WG;          // integer part of WG
  int F = (WG- I)*10;  // float part of WG
  sensors[2][2] = byte((I + sensors[2][2])/2);
  sensors[2][3] = byte((F + sensors[2][3])/2);
  
}
void recivedData()
{
    int index=-1;
    code = "";
    code.concat(char(data[0]));
    code.concat(char(data[1]));
    
    if(code == "LD"){index = 0;}
    if(code == "LN"){index = 1;}
    if(code == "LS"){index = 2;}
    if(code == "LZ"){index = 3;}
    if(code == "TD"){index = 4;}
    if(code == "TN"){index = 5;}
    if(code == "CD"){index = 6;}
    if(code == "CN"){index = 7;}
    if(code == "CS"){index = 8;}
    if(code == "CZ"){index = 9;}
    if(code == "CO"){index = 10;}
    if(code == "WP"){index = 11;}
    if(code == "WG"){index = 12;}
    if(index != -1) // code is for setting
    {
      settings[index][0] = data[0];
      settings[index][1] = data[1];
      settings[index][2] = data[2];
      settings[index][3] = data[3];
      settings[index][4] = data[4];
    }
    if(code == "DL"){sendAll();}
    if(code == "DO"){sendOne();}
    if(code == "DS"){sendSensor();}
    if(code == "CC")  // Set hour and minute on clock while keeping date (not used)
    {
      if(data[2] < 24 && data[3] < 60)
      { clock.setDateTime(dt.year, dt.month, dt.day, data[2], data[3], 20);}
    }
    
    WriteEeprom(); // save recived settings to EEPROM
}
void sendAll()
{
    radio.stopListening();
    for(int i=0; i<13;i++)
    {
      data[0] = settings[i][0];
      data[1] = settings[i][1];
      data[2] = settings[i][2];
      data[3] = settings[i][3];
      data[4] = settings[i][4];
      radio.write( &data, sizeof(data));
      digitalWrite(ledPin, HIGH);
      delay(blinkDelay);
      digitalWrite(ledPin, LOW);
      delay(sendDelay);
      wdt_reset();
    }
    data[4] = byte(0);
    for(int i=0; i<4;i++)
    {
      data[0] = sensors[i][0];
      data[1] = sensors[i][1];
      data[2] = sensors[i][2];
      data[3] = sensors[i][3];
      
      radio.write( &data, sizeof(data));
      digitalWrite(ledPin, HIGH);
      delay(blinkDelay);
      digitalWrite(ledPin, LOW);
      delay(sendDelay);
      wdt_reset();
    }
    radio.startListening();
}
void sendOne()
{
    radio.stopListening();
    int i = int(data[2]);
    data[0] = settings[i][0];
    data[1] = settings[i][1];
    data[2] = settings[i][2];
    data[3] = settings[i][3];
    data[4] = settings[i][4];
    radio.write( &data, sizeof(data));
    radio.startListening();
}
void sendSensor()
{
    radio.stopListening();
    data[4] = byte(0);
    for(int i=0; i<4;i++)
    {
      data[0] = sensors[i][0];
      data[1] = sensors[i][1];
      data[2] = sensors[i][2];
      data[3] = sensors[i][3];
      
      radio.write( &data, sizeof(data));
      digitalWrite(ledPin, HIGH);
      delay(blinkDelay);
      digitalWrite(ledPin, LOW);
      delay(sendDelay);
      wdt_reset();
    }
    radio.startListening();
}
int dayPhase()
{
  boolean LedOFF = true;
  if ( (COhh == 200) && (COmm == 200) ){LedOFF = false;} // check if Led OFF setting is used
  
  int start,stop,now;
  
  now = (cHour*60)+cMinute;
  
  start = (CShh*60)+CSmm;
  stop = (CDhh*60)+CDmm;
  if (now>=start&&now<=stop)
  { return 2; } // we are in dawn phase
  start = stop;
  stop = (CZhh*60)+CZmm;
  if (now>=start&&now<=stop)
  { return 0; } // we are in day phase
  start = stop;
  stop = (CNhh*60)+CNmm;
  if (now>=start&&now<=stop)
  { return 3; } // we are in twilight phase
  
  if(LedOFF)
  {
    if(COhh<24) // check if Led off time is before midnight
    {
      start = stop;
      stop = (COhh*60)+COmm;
      if (now>=start&&now<=stop)
      { return 1; } // we are in night phase
      
      start = stop;
      stop = 24*60;
      if (now>=start&&now<=stop)
      { return -1; } // we are in led off phase (current time is before midnight)
      
      start = 0;
      stop = (CShh*60)+CSmm;
      if (now>=start&&now<=stop)
      { return -1; } // we are in led off phase (current time is after midnight)
      
    }
    else        // Led off time is after midnight
    {
      start = stop;
      stop = 24*60;
      if (now>=start&&now<=stop)
      { return 1; } // we are in night phase (current time is before midnight)
      
      start = 0;
      stop = (COhh*60)+COmm;
      if (now>=start&&now<=stop)
      { return 1; } // we are in night phase (current time is after midnight)
      
      start = (COhh*60)+COmm;
      stop = (CShh*60)+CSmm;
      if (now>=start&&now<=stop)
      { return -1; } // we are in led off phase (current time is after midnight)
      
    }
  }
  else // no led off phase, night phase lasts until dawn
  {
    start = stop;
    stop = 24*60;
    if (now>=start&&now<=stop)
      { return 1; } // we are in night phase (current time is before midnight)
      
    start = 0;
    stop = (CShh*60)+CSmm;
    if (now>=start&&now<=stop)
    { return 1; } // we are in night phase (current time is after midnight)
  }
 
 return -1;
}
void ReadEeprom()
{
  settings[0][0] = 'L';
  settings[0][1] = 'D';
  settings[0][2] = EEPROM.read(0);
  settings[0][3] = EEPROM.read(1);
  settings[0][4] = EEPROM.read(2);
  
  settings[1][0] = 'L';
  settings[1][1] = 'N';
  settings[1][2] = EEPROM.read(3);
  settings[1][3] = EEPROM.read(4);
  settings[1][4] = EEPROM.read(5);

  settings[2][0] = 'L';
  settings[2][1] = 'S';
  settings[2][2] = EEPROM.read(6);
  settings[2][3] = EEPROM.read(7);
  settings[3][4] = EEPROM.read(8);
  
  settings[3][0] = 'L';
  settings[3][1] = 'Z';
  settings[3][2] = EEPROM.read(9);
  settings[3][3] = EEPROM.read(10);
  settings[3][4] = EEPROM.read(11);

  settings[4][0] = 'T';
  settings[4][1] = 'D';
  settings[4][2] = EEPROM.read(12);
  settings[4][3] = EEPROM.read(13);

  settings[5][0] = 'T';
  settings[5][1] = 'N';
  settings[5][2] = EEPROM.read(14);
  settings[5][3] = EEPROM.read(15);

  settings[6][0] = 'C';
  settings[6][1] = 'D';
  settings[6][2] = EEPROM.read(16);
  settings[6][3] = EEPROM.read(17);

  settings[7][0] = 'C';
  settings[7][1] = 'N';
  settings[7][2] = EEPROM.read(18);
  settings[7][3] = EEPROM.read(19);
  
  settings[8][0] = 'C';
  settings[8][1] = 'S';
  settings[8][2] = EEPROM.read(20);
  settings[8][3] = EEPROM.read(21);

  settings[9][0] = 'C';
  settings[9][1] = 'Z';
  settings[9][2] = EEPROM.read(22);
  settings[9][3] = EEPROM.read(23);
  
  settings[10][0] = 'C';
  settings[10][1] = 'O';
  settings[10][2] = EEPROM.read(24);
  settings[10][3] = EEPROM.read(25);
  
  settings[11][0] = 'W';
  settings[11][1] = 'P';
  settings[11][2] = EEPROM.read(26);
  settings[11][3] = EEPROM.read(27);

  settings[12][0] = 'W';
  settings[12][1] = 'G';
  settings[12][2] = EEPROM.read(28);
  settings[12][3] = EEPROM.read(29);
    
  
}
void WriteEeprom()
{ 
  //LD
  EEPROM.write(0,settings[0][2]); 
  EEPROM.write(1,settings[0][3]); 
  EEPROM.write(2,settings[0][4]); 
  
  //LN
  EEPROM.write(3,settings[1][2]); 
  EEPROM.write(4,settings[1][3]); 
  EEPROM.write(5,settings[1][4]); 

  //LS
  EEPROM.write(6,settings[2][2]); 
  EEPROM.write(7,settings[2][3]); 
  EEPROM.write(8,settings[2][4]);
  
  //LZ
  EEPROM.write(9,settings[3][2]); 
  EEPROM.write(10,settings[3][3]); 
  EEPROM.write(11,settings[3][4]); 

  //TD
  EEPROM.write(12,settings[4][2]); 
  EEPROM.write(13,settings[4][3]); 

  //TN
  EEPROM.write(14,settings[5][2]); 
  EEPROM.write(15,settings[5][3]); 

  //CD
  EEPROM.write(16,settings[6][2]); 
  EEPROM.write(17,settings[6][3]); 

  //CN
  EEPROM.write(18,settings[7][2]); 
  EEPROM.write(19,settings[7][3]); 
  
  //CS
  EEPROM.write(20,settings[8][2]);
  EEPROM.write(21,settings[8][3]);

  //CZ
  EEPROM.write(22,settings[9][2]); 
  EEPROM.write(23,settings[9][3]);
  
  //CO
  EEPROM.write(24,settings[10][2]); 
  EEPROM.write(25,settings[10][3]);
  
  //WP
  EEPROM.write(26,settings[11][2]); 
  EEPROM.write(27,settings[11][3]); 

  //WG
  EEPROM.write(28,settings[12][2]); 
  EEPROM.write(29,settings[12][3]);
}
void DummySettings()
{
  settings[0][0] = 'L';
  settings[0][1] = 'D';
  settings[0][2] = byte(1);
  settings[0][3] = byte(1);
  settings[0][4] = byte(1);
  
  settings[1][0] = 'L';
  settings[1][1] = 'N';
  settings[1][2] = byte(0);
  settings[1][3] = byte(0);
  settings[1][4] = byte(1);
  
  settings[2][0] = 'L';
  settings[2][1] = 'S';
  settings[2][2] = byte(1);
  settings[2][3] = byte(1);
  settings[2][4] = byte(0);
  
  settings[3][0] = 'L';
  settings[3][1] = 'Z';
  settings[3][2] = byte(1);
  settings[3][3] = byte(0);
  settings[3][4] = byte(0);
  
  settings[4][0] = 'T';
  settings[4][1] = 'D';
  settings[4][2] = byte(25);
  settings[4][3] = byte(0);
  
  settings[5][0] = 'T';
  settings[5][1] = 'N';
  settings[5][2] = byte(25);
  settings[5][3] = byte(0);
  
  settings[6][0] = 'C';
  settings[6][1] = 'D';
  settings[6][2] = byte(8);
  settings[6][3] = byte(30);
  
  settings[7][0] = 'C';
  settings[7][1] = 'N';
  settings[7][2] = byte(20);
  settings[7][3] = byte(30);
  
  settings[8][0] = 'C';
  settings[8][1] = 'S';
  settings[8][2] = byte(6);
  settings[8][3] = byte(30);
  
  settings[9][0] = 'C';
  settings[9][1] = 'Z';
  settings[9][2] = byte(19);
  settings[9][3] = byte(00);
  
  settings[10][0] = 'C';
  settings[10][1] = 'O';
  settings[10][2] = byte(23);
  settings[10][3] = byte(59);
  
  settings[11][0] = 'W';
  settings[11][1] = 'P';
  settings[11][2] = byte(32);
  settings[11][3] = byte(0);
  
  settings[12][0] = 'W';
  settings[12][1] = 'G';
  settings[12][2] = byte(0);
  settings[12][3] = byte(0);
  
  WriteEeprom();
}
/*
void nrfPing()
{
   radio.stopListening();
   int i = int(data[2]);
   data[0] = 'P';
   data[1] = 'I';
   data[2] = 'N';
   data[3] = 'G';
   data[4] = ' ';
   radio.write( &data, sizeof(data));
   radio.startListening(); 
  
}
*/

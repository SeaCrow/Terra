// PC module, lock SOFTSPI in RF24_config.h before uploading !
String slString = "";

#include <SPI.h>
#include "RF24.h"
RF24 radio(7,8);
byte addresses[][6] = {"1Node","2Node"};

byte data[5];
boolean newData;
void setup() {
  
  Serial.begin(9600);
  radio.begin();
  //radio.setPALevel(RF24_PA_LOW);
  radio.setRetries(15,15);

  radio.openWritingPipe(addresses[1]); //PC
  radio.openReadingPipe(1,addresses[0]);

  radio.startListening();
  
  newData = false;

}

void loop() {

  
  if(Serial.available() > 0){
    slString = Serial.readString();
    if(slString.length() == 13){  //recived string data, 3 values
    
      data[0] = byte(slString.charAt(0));
      data[1] = byte(slString.charAt(1));
      data[2] = byte(slString.substring(2,5).toInt());
      data[3] = byte(slString.substring(6,9).toInt());
      data[4] = byte(slString.substring(10).toInt());
      
      newData = true;
    }
    if(slString.length() == 9){  //recived string data, 2 values
    
      data[0] = byte(slString.charAt(0));
      data[1] = byte(slString.charAt(1));
      data[2] = byte(slString.substring(2,5).toInt());
      data[3] = byte(slString.substring(6).toInt());
      data[4] = byte(0);
      
      newData = true;
    }
    slString = "";
  }
  if(newData){
    radio.stopListening();

  
    boolean ok = radio.write( &data, sizeof(data));
  
    if(ok){
      Serial.println("DS"); // Data sent
    }else{
      Serial.println("FS"); //Failed to send
    }
    radio.startListening();
    newData = false;
  }
  
  if(radio.available()){
    radio.read(&data, sizeof(data));
    
    slString = "";
    slString.concat(char(data[0]));
    slString.concat(char(data[1]));
    slString.concat(ByteToString(data[2]));
    slString.concat(".");
    slString.concat(ByteToString(data[3]));
    slString.concat(".");
    slString.concat(ByteToString(data[4]));
    
    Serial.println(slString);
  } 
    
    
}
String ByteToString(byte b)
{
  String str ="";
  
  if(b < 10)
  {
    str.concat("00");
  }else
  {
    if(b < 100)
    {
      str.concat("0");
    }
  }
  str.concat(int(b));
 return str; 
}


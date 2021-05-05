#include <Arduino.h>

void write(String value) {
   Serial.println("{\"t\": 1, \"d\": { \"m\": \"" + value +"\"}}");
}

void hello() {
   Serial.println("{\"t\": 0}");
}

#include <ArduinoJson.h>
#include "OPCODE.h"
#include "LogWriter.h"

const int deserialization_capacity = JSON_OBJECT_SIZE(2) + 2 * JSON_OBJECT_SIZE(1);
void setup() { Serial.begin(9600); }

void loop()
{
    if (Serial.available() > 0) {
        String message = Serial.readString();

        StaticJsonDocument<deserialization_capacity> deserialization_doc;
        DeserializationError err = deserializeJson(deserialization_doc, message);
        if (err) {
            Serial.println(err.c_str());
        }
        else {
            const int t = deserialization_doc["t"];

            OPCODE op = static_cast<OPCODE>(t);

            switch (op) {
            case HELLO:
                hello();
                break;
            case HAND_UPDATE:
                write("primljen hand update");
                break;
            default:
                write("Primljen nepoznat opcode ");
                break;
            }
        }
    }
}

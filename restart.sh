#!/bin/bash

for i in "Energize" "Energize.Commands" "Energize.Essentials" "Energize.Interfaces" "Energize.Steam"
do
  rm -rf "$i/bin"
  rm -rf "$i/obj"
done

while true
do
  dotnet run -c Release --project Energize
  sleep 0.1
done

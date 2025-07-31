#!/bin/bash

echo "🔍 Детальное сравнение методов BanUserForLongName"
echo "=================================================="

# Извлекаем методы и сохраняем во временные файлы
echo "📋 Извлекаем метод из MessageHandler..."
grep -A 50 "internal async Task BanUserForLongName" ClubDoorman/Handlers/MessageHandler.cs | head -n 50 > /tmp/mh_method.txt

echo "📋 Извлекаем метод из UserBanService..."
grep -A 50 "public async Task BanUserForLongName" ClubDoorman/Services/UserBanService.cs | head -n 50 > /tmp/ubs_method.txt

echo ""
echo "📊 Сравнение с помощью diff:"
echo "----------------------------"
diff -u /tmp/mh_method.txt /tmp/ubs_method.txt

echo ""
echo "📊 Сравнение с помощью wc (количество строк):"
echo "---------------------------------------------"
echo "MessageHandler: $(wc -l < /tmp/mh_method.txt) строк"
echo "UserBanService: $(wc -l < /tmp/ubs_method.txt) строк"

echo ""
echo "📊 Сравнение с помощью md5sum:"
echo "------------------------------"
echo "MessageHandler: $(md5sum /tmp/mh_method.txt | cut -d' ' -f1)"
echo "UserBanService: $(md5sum /tmp/ubs_method.txt | cut -d' ' -f1)"

# Очистка
rm /tmp/mh_method.txt /tmp/ubs_method.txt 
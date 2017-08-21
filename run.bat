start .\binaries\Publisher\OrderSite.exe
start .\binaries\OrderSaga\OrderSaga.exe
start .\binaries\SubscriberA\PaymentProcessor.exe


echo "Press enter to start another payment processor instance"
pause

start .\binaries\SubscriberB\PaymentProcessor.exe
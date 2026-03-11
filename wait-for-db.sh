#!/bin/bash

MAX_ATTEMPTS=30
ATTEMPT=0

# Esperar até que a porta 1433 do SQL Server esteja aberta
until nc -z nopcommerce_mssql_server 1433; do
  echo "A aguardar para que o BD inicie... Tentativa $ATTEMPT de $MAX_ATTEMPTS"
  ATTEMPT=$((ATTEMPT + 1))
  if [ $ATTEMPT -ge $MAX_ATTEMPTS ]; then
      echo "Banco de dados não iniciou a tempo!"
      exit 1
  fi
  sleep 5
done
echo "SQL Server pronto!"
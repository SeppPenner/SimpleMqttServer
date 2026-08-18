FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY publish .

ENV ASPNETCORE_URLS="http://*:5000" \
    ASPNETCORE_ENVIRONMENT="Production" \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=Europe/Berlin \
    LANG=de_DE.UTF-8

# A variable of the same ENV instruction is not expanded yet, LANGUAGE and LC_ALL
# would end up empty in one block with LANG.
ENV LANGUAGE=${LANG} \
    LC_ALL=${LANG}

CMD ["dotnet", "SimpleMqttServer.dll"]
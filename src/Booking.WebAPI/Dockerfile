FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Booking.WebAPI/Booking.WebAPI.csproj", "src/Booking.WebAPI/"]
COPY ["src/Booking.Application/Booking.Application.csproj", "src/Booking.Application/"]
COPY ["src/Booking.Infrastructure/Booking.Infrastructure.csproj", "src/Booking.Infrastructure/"]
COPY ["src/Booking.Domain/Booking.Domain.csproj", "src/Booking.Domain/"]
RUN dotnet restore "src/Booking.WebAPI/Booking.WebAPI.csproj"

COPY . .
WORKDIR "/src/src/Booking.WebAPI"
RUN dotnet build "Booking.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.WebAPI.dll"]

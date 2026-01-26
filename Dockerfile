# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy solution and project files
COPY VolleyballDashboard.sln .
COPY src/VolleyballDashboard.Core/VolleyballDashboard.Core.csproj src/VolleyballDashboard.Core/
COPY src/VolleyballDashboard.Infrastructure/VolleyballDashboard.Infrastructure.csproj src/VolleyballDashboard.Infrastructure/
COPY src/VolleyballDashboard.Web/VolleyballDashboard.Web.csproj src/VolleyballDashboard.Web/

# Restore dependencies
RUN dotnet restore -a $TARGETARCH

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish src/VolleyballDashboard.Web/VolleyballDashboard.Web.csproj \
    -a $TARGETARCH \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:PublishTrimmed=false

# Runtime stage - using slim image for smaller size
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install minimal dependencies for Alpine
RUN apk add --no-cache icu-libs

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8 \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Copy published app
COPY --from=build /app/publish .

# Create non-root user for security
RUN adduser -D -u 1000 appuser
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/ || exit 1

# Entry point
ENTRYPOINT ["dotnet", "VolleyballDashboard.Web.dll"]

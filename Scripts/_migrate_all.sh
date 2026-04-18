#!/bin/bash
# Script chạy migration cho tất cả services

echo "=== HKShop Microservices - Migration Script ==="

services=("UserService" "ProductService" "CartService" "OrderService" "InvoiceService")

for service in "${services[@]}"; do
    echo ""
    echo "--- Migrating $service ---"
    cd "$service" || { echo "Cannot enter $service directory"; exit 1; }
    
    dotnet ef migrations add InitialCreate --no-build 2>/dev/null || echo "(Migration already exists, skipping...)"
    dotnet ef database update
    
    cd ..
    echo "✓ $service migration complete"
done

echo ""
echo "=== All migrations complete! ==="

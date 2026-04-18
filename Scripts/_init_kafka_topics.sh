#!/bin/bash
# Script tạo tất cả Kafka topics cần thiết

KAFKA_HOST="${KAFKA_HOST:-localhost:9092}"

echo "=== Creating HKShop Kafka Topics ==="
echo "Kafka: $KAFKA_HOST"

topics=(
    "user.registered"
    "user.updated"
    "customer.created"
    "customer.updated"
    "employee.created"
    "product.created"
    "product.updated"
    "product.deleted"
    "cart.updated"
    "cart.cleared"
    "order.created"
    "order.updated"
    "order.cancelled"
    "invoice.created"
    "invoice.exported"
)

for topic in "${topics[@]}"; do
    echo "Creating topic: $topic"
    docker exec kafka kafka-topics --create \
        --bootstrap-server "$KAFKA_HOST" \
        --topic "$topic" \
        --partitions 3 \
        --replication-factor 1 \
        --if-not-exists
done

echo ""
echo "=== All Kafka topics created! ==="
docker exec kafka kafka-topics --list --bootstrap-server "$KAFKA_HOST"

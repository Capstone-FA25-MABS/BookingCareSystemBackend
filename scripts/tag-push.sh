#!/bin/bash

###############################################################################
# Tag and Push BookingCare Images to DockerHub
# Usage: ./tag-push.sh [VERSION]
# Example: ./tag-push.sh v1.0.0
###############################################################################

set -e

# Configuration
DOCKERHUB_USER="hiumx"
VERSION="${1:-latest}"  # Default to 'latest' if no version provided

echo "=========================================="
echo "Tag & Push Images to DockerHub"
echo "=========================================="
echo "DockerHub User: ${DOCKERHUB_USER}"
echo "Version: ${VERSION}"
echo "=========================================="
echo ""

# Check if version parameter is provided
if [ -z "$1" ]; then
    echo "⚠️  No version specified, using 'latest'"
    echo "💡 Usage: $0 <version>"
    echo "   Example: $0 v1.0.0"
    echo ""
fi

# Login to DockerHub
echo "🔐 Logging in to DockerHub..."
docker login || {
    echo "❌ Docker login failed!"
    exit 1
}
echo ""

# Get all bookingcare images (exclude already tagged hiumx images)
IMAGES=$(docker images --format "{{.Repository}}:{{.Tag}}" | grep "^bookingcare-" | grep -v "<none>" | sort -u)

if [ -z "$IMAGES" ]; then
    echo "❌ No bookingcare-* images found locally!"
    echo ""
    echo "Available images:"
    docker images | grep -E "REPOSITORY|bookingcare"
    echo ""
    echo "Please build images first or check image names."
    exit 1
fi

echo "📦 Found images to process:"
echo "$IMAGES" | nl
echo ""

# Counter
SUCCESS_COUNT=0
FAILED_COUNT=0
TOTAL_COUNT=$(echo "$IMAGES" | wc -l)

echo "=========================================="
echo "Processing ${TOTAL_COUNT} images..."
echo "=========================================="
echo ""

# Process each image
while IFS= read -r image; do
    # Extract image name and current tag
    IMAGE_NAME=$(echo "$image" | cut -d: -f1)
    CURRENT_TAG=$(echo "$image" | cut -d: -f2)
    
    # Create new image name with dockerhub user
    NEW_IMAGE_NAME="${DOCKERHUB_USER}/${IMAGE_NAME}"
    
    echo "----------------------------------------"
    echo "Image: ${IMAGE_NAME}"
    echo "----------------------------------------"
    
    # Tag with specified version
    echo "→ Tagging: ${NEW_IMAGE_NAME}:${VERSION}"
    if docker tag "${image}" "${NEW_IMAGE_NAME}:${VERSION}"; then
        echo "✓ Tagged successfully"
        
        # Push to DockerHub
        echo "→ Pushing: ${NEW_IMAGE_NAME}:${VERSION}"
        if docker push "${NEW_IMAGE_NAME}:${VERSION}"; then
            echo "✓ Pushed successfully"
            SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
            
            # Also tag and push as 'latest' if version is not 'latest'
            if [ "$VERSION" != "latest" ]; then
                echo "→ Tagging: ${NEW_IMAGE_NAME}:latest"
                docker tag "${image}" "${NEW_IMAGE_NAME}:latest"
                echo "→ Pushing: ${NEW_IMAGE_NAME}:latest"
                docker push "${NEW_IMAGE_NAME}:latest" > /dev/null 2>&1 && echo "✓ Latest tag pushed"
            fi
        else
            echo "❌ Push failed"
            FAILED_COUNT=$((FAILED_COUNT + 1))
        fi
    else
        echo "❌ Tag failed"
        FAILED_COUNT=$((FAILED_COUNT + 1))
    fi
    echo ""
done <<< "$IMAGES"

# Summary
echo "=========================================="
echo "SUMMARY"
echo "=========================================="
echo ""
echo "Total images processed: ${TOTAL_COUNT}"
echo "✓ Successfully pushed: ${SUCCESS_COUNT}"
if [ ${FAILED_COUNT} -gt 0 ]; then
    echo "❌ Failed: ${FAILED_COUNT}"
fi
echo ""
echo "Version: ${VERSION}"
if [ "$VERSION" != "latest" ]; then
    echo "Also tagged as: latest"
fi
echo ""
echo "🔍 Verify on DockerHub:"
echo "   https://hub.docker.com/u/${DOCKERHUB_USER}"
echo ""

# Show all tagged images locally
echo "📋 Local images with ${DOCKERHUB_USER}/${VERSION}:"
docker images | grep "${DOCKERHUB_USER}" | grep "${VERSION}"
echo ""
echo "=========================================="

# Exit with error if any failed
if [ ${FAILED_COUNT} -gt 0 ]; then
    exit 1
fi

PROJECT := FLStudioRPC.csproj
CONFIG ?= Release
RID ?= linux-x64

BUILD_DIR := build
PUBLISH_DIR := $(BUILD_DIR)/publish/$(RID)

.PHONY: all help restore build publish package clean

all: build

help:
	@echo "FL Studio Linux RPC"
	@echo ""
	@echo "Available targets:"
	@echo "  make                         Build the project"
	@echo "  make restore                 Restore .NET dependencies"
	@echo "  make build                   Build the project"
	@echo "  make publish                 Create a self-contained Linux build"
	@echo "  make package VERSION=X.Y.Z   Build DEB, RPM and Arch packages"
	@echo "  make clean                   Remove generated build files"
	@echo ""
	@echo "Variables:"
	@echo "  CONFIG=Release               Build configuration"
	@echo "  RID=linux-x64                .NET runtime identifier"
	@echo ""
	@echo "Examples:"
	@echo "  make publish"
	@echo "  make publish RID=linux-arm64"
	@echo "  make package VERSION=1.3.0"

restore:
	dotnet restore $(PROJECT)

build: restore
	dotnet build $(PROJECT) \
		-c $(CONFIG) \
		--no-restore

publish: restore
	mkdir -p "$(PUBLISH_DIR)"
	dotnet publish $(PROJECT) \
		-c $(CONFIG) \
		-r $(RID) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishDir="$(CURDIR)/$(PUBLISH_DIR)/"

package:
ifndef VERSION
	$(error VERSION is required. Example: make package VERSION=1.3.0)
endif
	./packaging/build-packages.sh $(VERSION)

clean:
	rm -rf "$(BUILD_DIR)"
	rm -rf dist
	dotnet clean $(PROJECT) -c $(CONFIG)

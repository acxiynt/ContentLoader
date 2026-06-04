OBJECT = __modloader.cpp
TARGET = genesis.swacl.0
OUT = ./out
FTYPE  = .so
ifeq ($(OS),Windows_NT)
	FTYPE := .dll
endif
FLAGS_DEBUG = -shared -std=gnu++17 -g -o0
FLAGS_PUBLISH = -shared -std=gnu++17 -o3 -s
debug:
	@mkdir -p "$(OUT)"
	$(CC) $(FLAGS_PUBLISH) $(OBJECT) -o $(OUT)/$(TARGET)$(FTYPE)
	.PHONY: clean
publish:
	@mkdir -p "$(OUT)"
	$(CC) $(FLAGS_PUBLISH) $(OBJECT) -o $(OUT)/$(TARGET)$(FTYPE)
	.PHONY: clean
clean:
	rm -f $(TARGET)
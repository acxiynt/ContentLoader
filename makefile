CC = g++
OBJECT = __modloader.cpp
TARGET = genesis.swacl.so
OUT = ./out
FTYPE  = .0
FLAGS_DEBUG = -shared -std=gnu++17 -g -o0 -shared
FLAGS_PUBLISH = -shared -std=gnu++17 -o3 -s -shared
.PHONY: clean
debug:
	@mkdir -p "$(OUT)"
	$(CC) $(FLAGS_DEBUG) $(OBJECT) -o $(OUT)/$(TARGET)$(FTYPE)
publish:
	@mkdir -p "$(OUT)"
	$(CC) $(FLAGS_PUBLISH) $(OBJECT) -o $(OUT)/$(TARGET)$(FTYPE)
clean:
	rm -f $(TARGET)
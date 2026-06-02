OBJECT = __modloader.cpp
TARGET = genesis.swacl.0
FTYPE  = .so
ifeq ($(OS),Windows_NT)
	FTYPE := .dll
endif
FLAGS_DEBUG = -shared -std=gnu++17 -g -o0
FLAGS_PUBLISH = -shared -std=gnu++17 -o3 -s
debug:
	$(CC) $(FLAGS_PUBLISH) $(OBJECT) -o $(TARGET)$(FTYPE)
	.PHONY: clean
publish:
	$(CC) $(FLAGS_PUBLISH) $(OBJECT) -o $(TARGET)$(FTYPE)
	.PHONY: clean
clean:
	rm -f $(TARGET)
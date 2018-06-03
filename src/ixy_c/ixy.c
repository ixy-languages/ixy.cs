#include <stdint.h>
#include <stdbool.h>
#include <unistd.h>
#include <stddef.h>
#include <stdio.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <linux/limits.h>
#include <sys/stat.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>

#define HUGE_PAGE_BITS 21
#define HUGE_PAGE_SIZE (1 << HUGE_PAGE_BITS)

uintptr_t virt_to_phys(void *virt) {
    printf("Received pointer %p\n", virt);
    long pagesize = sysconf(_SC_PAGESIZE);
	int fd = open("/proc/self/pagemap", O_RDONLY);
	// pagemap is an array of pointers for each normal-sized page
	lseek(fd, (uintptr_t) virt / pagesize * sizeof(uintptr_t), SEEK_SET);
	uintptr_t phy = 0;
	read(fd, &phy, sizeof(phy));
	close(fd);
	if (!phy) {
		printf("Failed to convert pointer\n");
        return -1;
	}
	// bits 0-54 are the page number
    uintptr_t phys = (phy & 0x7fffffffffffffULL) * pagesize + ((uintptr_t) virt) % pagesize;
    printf("C function says phys pointer %lu\n", phys);
	return phys;
}

void *dma_memory(size_t size, bool require_contiguous) {
    //size_t size = 4096*16;
    if(size % HUGE_PAGE_SIZE)
        size = ((size >> HUGE_PAGE_BITS) + 1) << HUGE_PAGE_BITS;
    if(require_contiguous && size > HUGE_PAGE_SIZE)
        return NULL;

    //TODO : Make sure name is unique
    char path[PATH_MAX];
    snprintf(path, PATH_MAX, "/mnt/huge/ixy-%d-%d", getpid(), 0);
    int fd = open(path, O_CREAT | O_RDWR, S_IRWXU);
    //perror("Open: ");
    if(!fd) {
        printf("Could not open hugepage file\n");
        return NULL;
    }

    void *virt_addr = (void*)mmap(NULL, size, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_HUGETLB, fd, 0);
    //perror("mmap failed");
    mlock(virt_addr, size);
    close(fd);
    unlink(path);

    return virt_addr;
}
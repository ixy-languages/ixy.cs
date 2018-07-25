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
#include <time.h>

#define HUGE_PAGE_BITS 21
#define HUGE_PAGE_SIZE (1 << HUGE_PAGE_BITS)

void *dma_memory(size_t size, bool require_contiguous) {
    //size_t size = 4096*16;
    if(size % HUGE_PAGE_SIZE)
        size = ((size >> HUGE_PAGE_BITS) + 1) << HUGE_PAGE_BITS;
    if(require_contiguous && size > HUGE_PAGE_SIZE)
        return NULL;

    //TODO IMPLEMENT: Properly make sure name is unique
    char path[PATH_MAX];
    snprintf(path, PATH_MAX, "/mnt/huge/ixy-%d-%d", getpid(), rand());
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
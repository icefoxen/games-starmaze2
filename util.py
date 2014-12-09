def enum(*sequential, **named):
    enums = dict(list(zip(sequential, list(range(len(sequential))))), **named)
    typ = type('Enum', (), enums)
    # BUGGO: Apparently this doesn't work.  For SOME fucked up reason.
    #typ.__iter__ = enums.iteritems
    return typ



def unique(seq, idfun=None):
    """Returns a copy of the list with duplicates removed.
Found at http://www.peterbe.com/plog/uniqifiers-benchmark ."""
    # order preserving
    if idfun is None:
        def idfun(x): return x
    seen = set()
    result = []
    for item in seq:
        marker = idfun(item)
        if marker in seen: continue
        seen.add(marker)
        result.append(item)
    return result

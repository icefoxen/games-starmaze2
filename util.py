def enum(*sequential, **named):
    enums = dict(list(zip(sequential, list(range(len(sequential))))), **named)
    typ = type('Enum', (), enums)
    # BUGGO: Apparently this doesn't work.  For SOME fucked up reason.
    #typ.__iter__ = enums.iteritems
    return typ



